using System;

public class DeathMatchEvents
{
    public class PlayerKilledEvent : GameModeEvent
    {
        public override Type EventType { get => typeof(PlayerKilledEvent); }

        public ulong VictimId { get; set; }
        public ulong KillerId { get; set; }
    }
}
