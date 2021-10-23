using Unity.Netcode;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MultiplayerGameMode : NetworkBehaviour
{
    protected Dictionary<ulong, GameManager> players = new Dictionary<ulong, GameManager>();

    protected Queue<GameModeEvent> events = new Queue<GameModeEvent>();

    public abstract string Name { get; }

    public void EmitEvent(GameModeEvent gameModeEvent)
    {
        events.Enqueue(gameModeEvent);
    }

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

    private void HandleEvents()
    {
        while(events.Count > 0)
        {
            HandleEvent(events.Dequeue());
        }
    }

    protected abstract void HandleEvent(GameModeEvent gmEvent);

    public virtual void Update()
    {
        HandleEvents();
    }

    public abstract Type GetExtensionsType();
}
