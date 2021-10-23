using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public partial class DeathMatchGameMode : MultiplayerGameMode
{
    public override string Name { get => "Deathmatch"; }

    protected override void HandleEvent(GameModeEvent gmEvent)
    {
        if (gmEvent is DeathMatchEvents.PlayerKilledEvent)
        {
            DeathMatchEvents.PlayerKilledEvent playerKilledEvent = gmEvent as DeathMatchEvents.PlayerKilledEvent;

            // Send a kill feed update.
            using(FastBufferWriter writer = new FastBufferWriter(sizeof(ulong) * 2, Unity.Collections.Allocator.Temp))
            {
                // Write player IDs to the buffer.
                writer.WriteValueSafe(playerKilledEvent.VictimId);
                writer.WriteValueSafe(playerKilledEvent.KillerId);

                // Send the update as a named message to all clients.
                InvokeGlobalEvent(DeathMatchGameManagerExtensions.KillFeedMessageHandlerName, writer);
            }
        }
    }

    public override Type GetExtensionsType() => typeof(DeathMatchGameManagerExtensions);
}
