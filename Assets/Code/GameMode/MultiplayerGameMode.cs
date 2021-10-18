using MLAPI;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MultiplayerGameMode
{
    protected Dictionary<ulong, GameManager> players = new Dictionary<ulong, GameManager>();

    protected Queue<GameModeEvent> events = new Queue<GameModeEvent>();

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

    public virtual void HandleEvents()
    {

    }

    protected abstract Type GetExtensionsType();

    /// <summary>
    /// Sets up the gamemode for the client.
    /// </summary>
    /// <param name="gameManager"></param>
    public void CreateExtensionsForPlayer(GameManager gameManager)
    {
        GameModeGameManagerExtension.Create(GetExtensionsType(), gameManager);
    }
}
