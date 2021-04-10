using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerScore
{
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Cleanups { get; set; }

    public static PlayerScore FromString(string src)
    {
        try
        {
            string[] values = src.Split(',');
            int.TryParse(values[0], out int kills);
            int.TryParse(values[1], out int deaths);
            int.TryParse(values[2], out int cleanups);

            return new PlayerScore { Kills = kills, Deaths = deaths, Cleanups = cleanups };
        }
        catch(Exception)
        {
            return new PlayerScore();
        }
    }

    public override string ToString() => $"{Kills},{Deaths},{Cleanups}";

    const float KillMultiplier = 1.5f;
    const float CleanupMultiplier = 1f;

    public int KillPoints { get { return (int)Mathf.Round(Kills * KillMultiplier); } }
    public int CleanupPoints { get { return (int)Mathf.Round(Cleanups * CleanupMultiplier); } }
    public int TotalPoints { get { return KillPoints + CleanupPoints; } }
}