using System;

/// <summary>
/// Event structures for the Deathmatch gamemode.
/// </summary>
public class DeathMatchEvents
{
    /// <summary>
    /// Event fired when a player dies, containing the client IDs of the victim and killer.
    /// </summary>
    public class PlayerKilledEvent : GameModeEvent
    {
        public override Type EventType { get => typeof(PlayerKilledEvent); }

        public ulong VictimId { get; set; }
        public ulong KillerId { get; set; }
    }
}
