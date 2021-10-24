using Unity.Netcode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public partial class DeathMatchGameMode
{
    public class DeathMatchGameManagerExtensions : GameModeGameManagerExtension
    {
        /// <summary>
        /// UI text objects.
        /// </summary>
        public Text kdpText, winnerText, killFeedText;

        private string KillLog = "";
        public int MaxKillLogLines = 5;

        public const string KillFeedMessageHandlerName = "DM_killFeedUpdate";
        public const string GameOverMessageHandlerName = "DM_gameOver";

        public bool IsInProgress = true;
        public bool IsGameOver = false;

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
                }

                if (kdpText && winnerText && killFeedText)
                {
                    break;
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            InitialiseUI();

            IsInProgress = true;
            IsGameOver = false;

            if (IsOwner)
            {
            }
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
        /// Updates the values of the stats text.
        /// </summary>
        protected virtual void UpdateStatsHud(bool hide = false)
        {
            if(hide)
            {
                kdpText.text = "";
                winnerText.text = "";
                killFeedText.text = "";
                healthText.text = "";

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

            killFeedText.text = KillLog;
        }

        protected override void UpdateHud()
        {
            base.UpdateHud();

            DisplayGameOverScreen(IsInProgress);
            UpdateStatsHud(IsGameOver);
        }

        protected override void AssignEventHandlers()
        {
            EventHandlers[KillFeedMessageHandlerName] = KillFeedMessageHandler;
            EventHandlers[GameOverMessageHandlerName] = GameOverMessageHandler;
        }
    }
}