using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerScore
{
    public int Kills { get; set; }
    public int Cleanups { get; set; }

    const float KillMultiplier = 1.5f;
    const float CleanupMultiplier = 1f;

    public int KillPoints { get { return (int)Mathf.Round(Kills * KillMultiplier); } }
    public int CleanupPoints { get { return (int)Mathf.Round(Cleanups * CleanupMultiplier); } }
    public int TotalPoints { get { return KillPoints + CleanupPoints; } }
}