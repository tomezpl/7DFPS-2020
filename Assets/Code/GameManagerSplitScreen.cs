using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public partial class GameManager
{
    public List<GameManager> OwnedSplitScreenClients = new List<GameManager>();

    public bool IsSplitScreenOwner = false;

    public GameManager SplitScreenOwner = null;

    private int TotalSplitScreenPlayers { get => (SplitScreenOwner?.OwnedSplitScreenClients?.Count ?? 0) + 1; }

    public int SplitScreenIndex = 0;

    public Rect GetSplitScreenRect(bool twoPlayerHorizontal = false)
    {
        Debug.Log($"Calling GetSplitScreenREct for player index {SplitScreenIndex}, SplitScreenPlayerCount: {TotalSplitScreenPlayers}");

        if(TotalSplitScreenPlayers == 1 || TotalSplitScreenPlayers > 4)
        {
            return new Rect(0f, 0f, 1f, 1f);
        }

        return TotalSplitScreenPlayers switch
        {
            2 => twoPlayerHorizontal ? new Rect(0f, 0f + 0.51f * SplitScreenIndex, 1f, 0.49f) : new Rect(0f + 0.505f * SplitScreenIndex, 0f, 0.495f, 1f),
            3 => SplitScreenIndex < 2 ? new Rect(0f + 0.505f * SplitScreenIndex, 0f, 0.495f, 0.49f) : new Rect(0f, 0.51f, 1f, 0.49f),
            4 => new Rect(0f + 0.505f * ((SplitScreenIndex + 1) % 2f), SplitScreenIndex > 1 ? 0.51f : 0f, 0.495f, 0.49f),
            _ => throw new ArgumentException("TotalSplitScreenPlayers", $"Split screen playercount {TotalSplitScreenPlayers} was invalid.")
        };
    }

    public void SetupSplitScreen(int playerIndex, GameManager owner)
    {
        if(owner.GetInstanceID() == GetInstanceID())
        {
            IsSplitScreenOwner = true;
        }
        else
        {
            SplitScreenOwner = owner;
        }

        SplitScreenIndex = playerIndex;
    }
}
