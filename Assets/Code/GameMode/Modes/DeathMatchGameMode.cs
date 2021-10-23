using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

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
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(DeathMatchGameManagerExtensions.KillFeedMessageHandlerName, writer);
            }

            GameModeGameManagerExtension serverGmExt = GameManager.FromId(NetworkManager.Singleton.ServerClientId)?.GameModeExtensions;
            if(serverGmExt && serverGmExt is DeathMatchGameManagerExtensions)
            {
                (serverGmExt as DeathMatchGameManagerExtensions).UpdateKillFeed(playerKilledEvent.VictimId, playerKilledEvent.KillerId);
            }
        }
    }

    public override Type GetExtensionsType() => typeof(DeathMatchGameManagerExtensions);
}
