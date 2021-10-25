using System;

/// <summary>
/// An event that can be fired by the server-side gamemode script to client-side gamemode extension scripts.
/// </summary>
public abstract class GameModeEvent
{
    /// <summary>
    /// Gets the type of this event. Used to cast.
    /// </summary>
    public abstract Type EventType { get; }
}