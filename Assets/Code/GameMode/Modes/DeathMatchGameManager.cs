using Unity.Netcode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

public partial class DeathMatchGameMode
{
    /// <summary>
    /// Client-side extensions for the deathmatch gamemode.
    /// </summary>
    public class DeathMatchGameManagerExtensions : GameModeGameManagerExtension
    {
        /// <summary>
        /// UI text objects.
        /// </summary>
        public Text kdpText, winnerText, killFeedText, timerText;

        /// <summary>
        /// String containing lines in the kill feed.
        /// </summary>
        private string KillLog = "";

        /// <summary>
        /// Maximum number of lines shown at once in the kill feed.
        /// </summary>
        public int MaxKillLogLines = 5;

        /// <summary>
        /// Netcode message handler names.
        /// </summary>
        public const string KillFeedMessageHandlerName = "DM_killFeedUpdate",
                            GameOverMessageHandlerName = "DM_gameOver",
                            TimerUpdateMessageName = "DM_timerTick";

        /// <summary>
        /// Match state.
        /// </summary>
        public bool IsInProgress = true,
                    IsGameOver = false;

        /// <summary>
        /// String with mm:ss formatted time.
        /// </summary>
        private string TimeRemainingString = "";

        public override void InitialiseUI()
        {
            base.InitialiseUI();

            // Find the UI text objects in the scene.
            foreach (Text text in GameObject.Find("HUD").GetComponentsInChildren<Text>())
            {
                switch (text.name)
                {
                    case "DM_KDP":
                        kdpText = text;
                        break;
                    case "DM_Winner":
                        winnerText = text;
                        break;
                    case "DM_KillFeedLog":
                        killFeedText = text;
                        break;
                    case "TimerText":
                        timerText = text;
                        break;
                }

                if (kdpText && winnerText && killFeedText && timerText)
                {
                    break;
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            InitialiseUI();

            // Initialise match state.
            IsInProgress = true;
            IsGameOver = false;
        }

        private void GameOverMessageHandler(ulong senderClientId, FastBufferReader messagePayload)
        {
            if (!IsOwner)
            {
                return;
            }

            messagePayload.ReadValueSafe(out bool scoreReached);
            messagePayload.ReadValueSafe(out bool hasWinnerId);
            ulong winnerId = ulong.MaxValue;
            if(hasWinnerId)
            {
                messagePayload.ReadValueSafe(out winnerId);
            }

            IsInProgress = false;
            IsGameOver = true;

            RequestGameOverScreen(scoreReached, hasWinnerId ? winnerId : new ulong?());

            if (Owner.SpawnedPlayer)
            {
                Owner.SpawnedPlayer.LockInput = true;
            }
        }

        /// <summary>
        /// Prepare the "game over" screen.
        /// </summary>
        /// <param name="scoreReached">Did the game end because the score was reached? If false, out of time is assumed.</param>
        /// <param name="winnerId">ID of the winning player (highest score)</param>
        private void RequestGameOverScreen(bool scoreReached, ulong? winnerId)
        {
            string subText = scoreReached ? "Target score reached. " : "Ran out of time. ";
            if (winnerId != null)
            {
                GameManager winnerGm = GameManager.FromId(winnerId.Value);
                if (winnerGm)
                {
                    subText += $"{winnerGm.PlayerName.Value} wins with {winnerGm.Score.TotalPoints} points!";
                }
            }
            GameOverAlert = ("GAME OVER!", subText);
        }

        /// <summary>
        /// Update the UI to display or hide the "game over" screen.
        /// </summary>
        /// <param name="hide">Should the game over screen be hidden?</param>
        private void DisplayGameOverScreen(bool hide = false)
        {
            gameOverMainText.text = hide ? "" : GameOverAlert.MainText;
            gameOverSubText.text = hide ? "" : GameOverAlert.SubText;
        }

        private void KillFeedMessageHandler(ulong senderClientId, FastBufferReader messagePayload)
        {
            Debug.Log("Begin KillFeedUpdate");

            messagePayload.ReadValueSafe(out ulong victimId);
            messagePayload.ReadValueSafe(out ulong killerId);

            UpdateKillFeed(victimId, killerId);
        }

        /// <summary>
        /// Update the kill feed upon a <see cref="DeathMatchEvents.PlayerKilledEvent"/>.
        /// </summary>
        /// <param name="victimId">Victim client ID from the event.</param>
        /// <param name="killerId">Killer client ID from the event. Can be the same as the victim, in which case it's considered a suicide.</param>
        public void UpdateKillFeed(ulong victimId, ulong killerId)
        {
            Debug.Log($"Received UpdateKillLogClientRpc with params ({victimId}, {killerId})");

            string victimName = victimId == killerId ? "themselves" : GameManager.FromId(victimId)?.PlayerName?.Value.ToString() ?? "UNKNOWN_PLAYER";
            string killerName = GameManager.FromId(killerId)?.PlayerName?.Value.ToString() ?? "UNKNOWN_PLAYER";

            string killMessage = $"{killerName} destroyed {victimName}!";

            KillLog = !string.IsNullOrWhiteSpace(KillLog) ? $"{KillLog}\n{killMessage}" : killMessage;

            if (KillLog.Count(c => c == '\n') > MaxKillLogLines)
            {
                KillLog = KillLog.Substring(KillLog.IndexOf('\n') + 1);
            }
        }



        /// <summary>
        /// Updates the values of the stats text or hides it.
        /// </summary>
        /// <param name="hide">Should the stats HUD be hidden?</param>
        protected virtual void UpdateStatsHud(bool hide = false)
        {
            if(hide)
            {
                kdpText.text = "";
                winnerText.text = "";
                killFeedText.text = "";
                healthText.text = "";
                timerText.text = "";

                return;
            }

            // Update local player's stats text.
            PlayerScore score = Owner.Score;
            kdpText.text = $"{score.Kills} Kills, {score.Deaths} Deaths, {score.TotalPoints} Points";

            // Find currently winning player to display their name and points.
            Dictionary<string, PlayerScore> players = PlayerScores;
            string winner = players.Keys.FirstOrDefault(name => !players.Any(p => p.Key != name && p.Value.TotalPoints > players[name].TotalPoints));
            if (players.Count == 1)
            {
                winner = players.Keys.FirstOrDefault();
            }
            if (winner != default)
            {
                winnerText.text = $"1st place: {winner} ({players[winner].TotalPoints} points)";
            }
            else
            {
                winnerText.text = "";
            }

            timerText.text = TimeRemainingString;

            killFeedText.text = KillLog;
        }

        protected override void UpdateHud()
        {
            base.UpdateHud();

            DisplayGameOverScreen(IsInProgress);
            UpdateStatsHud(IsGameOver);
        }

        private void TimerUpdateMessageHandler(ulong senderClientId, FastBufferReader messagePayload)
        {
            messagePayload.ReadValueSafe(out long ticks);
            TimeSpan remaining = TimeSpan.FromTicks(ticks);
            int seconds = remaining.Seconds;
            int minutes = remaining.Minutes;
            TimeRemainingString = $"{(minutes < 10 ? $"0{minutes}" : $"{minutes}")}:{(seconds < 10 ? $"0{seconds}" : $"{seconds}")}";
        }

        protected override void AssignEventHandlers()
        {
            EventHandlers[KillFeedMessageHandlerName] = KillFeedMessageHandler;
            EventHandlers[GameOverMessageHandlerName] = GameOverMessageHandler;
            EventHandlers[TimerUpdateMessageName] = TimerUpdateMessageHandler;
        }
    }
}