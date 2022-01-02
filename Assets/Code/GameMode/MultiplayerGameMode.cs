using Unity.Netcode;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Server-side script containing gamemode logic.
/// </summary>
public abstract class MultiplayerGameMode : NetworkBehaviour
{
    /// <summary>
    /// Players taking part in the gamemode.
    /// </summary>
    protected Dictionary<ulong, GameManager> players = new Dictionary<ulong, GameManager>();

    /// <summary>
    /// Event queue.
    /// </summary>
    protected Queue<GameModeEvent> events = new Queue<GameModeEvent>();

    /// <summary>
    /// Gamemode name.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Adds an event to the event queue.
    /// </summary>
    /// <param name="gameModeEvent">The event to add.</param>
    public void EmitEvent(GameModeEvent gameModeEvent)
    {
        events.Enqueue(gameModeEvent);
    }

    /// <summary>
    /// Sends an event as a named message to all clients + server.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="writer"></param>
    public void InvokeGlobalEvent(string eventName, FastBufferWriter writer)
    {
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(eventName, writer);

        // Also invoke on host.
        GameModeGameManagerExtension serverGmExt = GameManager.FromId(NetworkManager.Singleton.ServerClientId)?.GameModeExtensions;
        if (serverGmExt) 
        {
            using (FastBufferReader reader = new FastBufferReader(writer, Unity.Collections.Allocator.Temp))
            {
                serverGmExt.EventHandlers[eventName](NetworkManager.Singleton.ServerClientId, reader);
            }
        }
    }

    /// <summary>
    /// Acknowledges a player joining the gamemode.
    /// </summary>
    /// <param name="player">The joining player's <see cref="GameManager"/>.</param>
    public void AddPlayer(GameManager player)
    {
        players[player.OwnerClientId] = player;
    }

    /// <summary>
    /// Acknowledges a player leaving the gamemode.
    /// </summary>
    /// <param name="playerClientId">The leaving player's client ID.</param>
    public void RemovePlayer(ulong playerClientId)
    {
        if(players.ContainsKey(playerClientId))
        {
            players.Remove(playerClientId);
        }
    }

    /// <summary>
    /// Iterates through the event queue.
    /// </summary>
    private void HandleEvents()
    {
        while(events.Count > 0)
        {
            HandleEvent(events.Dequeue());
        }
    }

    /// <summary>
    /// Handles an individual event.
    /// </summary>
    /// <param name="gmEvent">The event to handle.</param>
    protected abstract void HandleEvent(GameModeEvent gmEvent);

    public virtual void Update()
    {
        HandleEvents();
    }

    /// <summary>
    /// Provides the class type of the relevant client-side extensions script for this game mode.
    /// </summary>
    /// <returns>Returns the applicable <see cref="GameModeGameManagerExtension"/> type.</returns>
    public abstract Type GetExtensionsType();
}
