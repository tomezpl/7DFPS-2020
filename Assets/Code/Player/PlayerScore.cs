using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Container class for storing the player's score details.
/// </summary>
public class PlayerScore
{
    /// <summary>
    /// The player's awarded kills.
    /// </summary>
    public int Kills { get; set; }

    /// <summary>
    /// The player's suffered deaths.
    /// </summary>
    public int Deaths { get; set; }

    /// <summary>
    /// The player's collected cleanups.
    /// </summary>
    public int Cleanups { get; set; }

    /// <summary>
    /// Construct a <see cref="PlayerScore"/> from a serialized format.
    /// </summary>
    /// <param name="src">Stringified <see cref="PlayerScore"/>, usually retrieved from a <see cref="MLAPI.NetworkVariable.NetworkVariableString"/>.</param>
    /// <returns>Returns a deserialized object based on <paramref name="src"/>.</returns>
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

    /// <summary>
    /// Serialize (stringify?) this <see cref="PlayerScore"/>.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{Kills},{Deaths},{Cleanups}";

    /// <summary>
    /// Multiplier for each kill.
    /// </summary>
    const float KillMultiplier = 1.5f;

    /// <summary>
    /// Multiplier for each cleanup collection.
    /// </summary>
    const float CleanupMultiplier = 1f;

    /// <summary>
    /// Total points from kills, with the kill multiplier applied for each kill.
    /// </summary>
    public int KillPoints { get { return (int)Mathf.Round(Kills * KillMultiplier); } }

    /// <summary>
    /// Total points from cleanup collections, with the cleanup multiplier applied for each collection.
    /// </summary>
    public int CleanupPoints { get { return (int)Mathf.Round(Cleanups * CleanupMultiplier); } }

    /// <summary>
    /// Total points combined.
    /// </summary>
    public int TotalPoints { get { return KillPoints + CleanupPoints; } }
}