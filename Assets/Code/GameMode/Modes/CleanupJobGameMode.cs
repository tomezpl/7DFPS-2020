using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

public class CleanupJobGameMode : DeathMatchGameMode
{
    public override string Name => "Cleanup Job";

    protected override void HandleEvent(GameModeEvent gmEvent)
    {
        base.HandleEvent(gmEvent);

        if(gmEvent is CleanupJobEvents.PlayerCleanupEvent)
        {
            CleanupJobEvents.PlayerCleanupEvent playerCleanupEvent = gmEvent as CleanupJobEvents.PlayerCleanupEvent;

            using(FastBufferWriter writer = new FastBufferWriter(sizeof(ulong) * 2, Unity.Collections.Allocator.Temp))
            {
                writer.WriteValueSafe(playerCleanupEvent.CollectorId);
                writer.WriteValueSafe(playerCleanupEvent.DestroyedRoombaId);

                InvokeGlobalEvent(CleanupJobGameManager.PlayerCleanupMessageHandlerName, writer);
            }
        }
        else if(gmEvent is CleanupJobEvents.MessCleanupEvent)
        {
            CleanupJobEvents.MessCleanupEvent messCleanupEvent = gmEvent as CleanupJobEvents.MessCleanupEvent;

            if(messCleanupEvent.MessRef.TryGet(out CollectableMess mess))
            {
                mess.CollectServerRpc(messCleanupEvent.CollectorId);
            }
        }
    }
}
