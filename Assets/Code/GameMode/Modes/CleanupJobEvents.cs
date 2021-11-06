using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

public class CleanupJobEvents : DeathMatchEvents
{
    public class PlayerCleanupEvent : MessCleanupEvent
    {
        public override Type EventType { get => typeof(PlayerCleanupEvent); }

        public ulong DestroyedRoombaId { get; set; }
    }

    public class MessCleanupEvent : GameModeEvent
    {
        public override Type EventType { get => typeof(MessCleanupEvent); }

        public ulong CollectorId { get; set; }
        public NetworkBehaviourReference MessRef { get; set; }
    }
}