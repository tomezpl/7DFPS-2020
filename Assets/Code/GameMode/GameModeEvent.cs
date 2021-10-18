using System;

public abstract class GameModeEvent
{
    /// <summary>
    /// Gets the type of this event. Used to cast.
    /// </summary>
    public abstract Type EventType { get; }
}