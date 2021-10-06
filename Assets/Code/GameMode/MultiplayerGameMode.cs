using MLAPI;
using MLAPI.NetworkVariable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class MultiplayerGameMode : NetworkBehaviour
{
    protected Dictionary<ulong, GameManager> players = new Dictionary<ulong, GameManager>();

    public abstract string Name { get; }

    public void AddPlayer(GameManager player)
    {
        players[player.OwnerClientId] = player;
    }

    public void RemovePlayer(ulong playerClientId)
    {
        if(players.ContainsKey(playerClientId))
        {
            players.Remove(playerClientId);
        }
    }
}
