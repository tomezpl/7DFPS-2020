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
        public Text kdpText, winnerText;

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
                }

                if (kdpText && winnerText)
                {
                    break;
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            InitialiseUI();
        }



        /// <summary>
        /// Updates the values of the stats text.
        /// </summary>
        protected virtual void UpdateStatsHud()
        {
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
        }

        protected override void UpdateHud()
        {
            base.UpdateHud();

            UpdateStatsHud();
        }
    }
}