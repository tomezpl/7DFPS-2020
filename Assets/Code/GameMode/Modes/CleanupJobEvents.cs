using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

/// <summary>
/// Events that can occur during a Cleanup Job match.
/// </summary>
public class CleanupJobEvents : DeathMatchEvents
{
    /// <summary>
    /// Event fired when a player collects another player's destroyed Roomba.
    /// </summary>
    public class PlayerCleanupEvent : MessCleanupEvent
    {
        public override Type EventType { get => typeof(PlayerCleanupEvent); }

        /// <summary>
        /// Client ID of the destroyed Roomba's owner.
        /// </summary>
        public ulong DestroyedRoombaId { get; set; }
    }

    /// <summary>
    /// Event fired when a player collects mess from the floor.
    /// </summary>
    public class MessCleanupEvent : GameModeEvent
    {
        public override Type EventType { get => typeof(MessCleanupEvent); }

        /// <summary>
        /// Client ID of the Roomba that collected this mess.
        /// </summary>
        public ulong CollectorId { get; set; }

        /// <summary>
        /// The <see cref="CollectableMess"/> that triggered this event.
        /// </summary>
        public NetworkBehaviourReference MessRef { get; set; }
    }
}