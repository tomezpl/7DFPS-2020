using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class CollectableMess : NetworkBehaviour
{
    NetworkVariable<bool> CanBePickedUp = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone, true);

    bool couldBePickedUpLastFrame = true;
    float disappearTimer = 0f;
    bool startDisappearing = false;
    float disappearTime = 3f;

    public bool IsPlayerCorpse = false;
    public ulong DestroyedRoombaId = 0;

    [ServerRpc]
    public void CollectServerRpc(ulong collectorId, ServerRpcParams rpcParams = default)
    {
        GameManager collector = GameManager.FromId(collectorId);

        if (collector != null)
        {
            CanBePickedUp.Value = false;

            collector.GiveScoreCleanupsServerRpc(1);
        }
    }

    public void Update()
    {
        if(!CanBePickedUp.Value && couldBePickedUpLastFrame)
        {
            startDisappearing = true;
        }

        if(disappearTimer >= disappearTime)
        {

            if(IsServer)
            {
                Destroy(gameObject);
            }
        }

        disappearTimer += startDisappearing ? Time.deltaTime : 0f;

        couldBePickedUpLastFrame = CanBePickedUp.Value;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (IsServer && CanBePickedUp.Value)
        {
            RoombaControl roomba = other.GetComponentInParent<RoombaControl>();

            if (roomba)
            {
                if (!IsPlayerCorpse)
                {
                    LobbyManager.Singleton.CurrentGameMode.EmitEvent(new CleanupJobEvents.MessCleanupEvent
                    {
                        CollectorId = roomba.OwnerClientId,
                        MessRef = new NetworkBehaviourReference(this)
                    });
                }
                else
                {
                    LobbyManager.Singleton.CurrentGameMode.EmitEvent(new CleanupJobEvents.PlayerCleanupEvent
                    {
                        CollectorId = roomba.OwnerClientId,
                        DestroyedRoombaId = DestroyedRoombaId,
                        MessRef = new NetworkBehaviourReference(this)
                    });
                }
            }
        }
    }
}
