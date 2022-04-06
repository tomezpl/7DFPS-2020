using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A script used for objects placed on the floor that can be collected by the players.
/// </summary>
public class CollectableMess : NetworkBehaviour
{
    /// <summary>
    /// Possible sound effects to play when the Roomba collects the mess.
    /// </summary>
    public AudioSource[] SuckNoises = new AudioSource[0];

    /// <summary>
    /// Should players be allowed to collect this object?
    /// </summary>
    protected NetworkVariable<bool> CanBePickedUp = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone, true);

    /// <summary>
    /// <see cref="RoombaControl"/> that collected this object.
    /// </summary>
    NetworkVariable<NetworkBehaviourReference> Collector = new NetworkVariable<NetworkBehaviourReference>(NetworkVariableReadPermission.Everyone, new NetworkBehaviourReference());

    /// <summary>
    /// Has the object already been collected by a <see cref="RoombaControl"/>?
    /// </summary>
    NetworkVariable<bool> AlreadyPickedUp = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone, false);

    /// <summary>
    /// The object's position at the moment of being collected.
    /// </summary>
    NetworkVariable<Vector3> SuckOrigin = new NetworkVariable<Vector3>(NetworkVariableReadPermission.Everyone, Vector3.zero);

    /// <summary>
    /// Was <see cref="CanBePickedUp"/> true last frame?
    /// </summary>
    protected bool couldBePickedUpLastFrame = true;

    float disappearTimer = 0f;
    bool startDisappearing = false;
    float disappearTime = 1.5f;

    /// <summary>
    /// Is the object a Roomba's remains?
    /// </summary>
    public bool IsPlayerCorpse = false;

    /// <summary>
    /// Client ID of the dead Roomba's owner (applicable if <see cref="IsPlayerCorpse"/> is true)
    /// </summary>
    public ulong DestroyedRoombaId = 0;

    /// <summary>
    /// Distance limit for the pull in animation.
    /// </summary>
    public float MaxDistanceFromCollector = 0.75f;

    /// <summary>
    /// True if the object scale should move towards zero during the animation, false if only half.
    /// </summary>
    public bool FullSuck = false;

    Vector3 initScale = Vector3.one;

    bool alreadyPickedUp = false;

    RoombaControl collectorRoomba = null;

    protected virtual void Start()
    {
        initScale = transform.localScale;
    }

    [ServerRpc]
    public void CollectServerRpc(ulong collectorId, ServerRpcParams rpcParams = default)
    {
        GameManager collector = GameManager.FromId(collectorId);

        if (collector != null)
        {
            CanBePickedUp.Value = false;

            collector.GiveScoreCleanupsServerRpc(1);
        }

        CollectClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds } });
    }

    [ClientRpc]
    public void CollectClientRpc(ClientRpcParams rpcParams = default)
    {
        StartPickupEffects();
    }

    public void StartPickupEffects()
    {
        startDisappearing = true;
        PlaySuckAudio();
    }

    /// <summary>
    /// Plays a random sound effect from <see cref="SuckNoises"/>.
    /// </summary>
    private void PlaySuckAudio()
    {
        int numNoises = SuckNoises.Length;

        if (numNoises != 0)
        {
            SuckNoises[Random.Range(0, numNoises)].Play();
        }
    }

    public virtual void Update()
    {
        if(!CanBePickedUp.Value && couldBePickedUpLastFrame && !startDisappearing)
        {
            StartPickupEffects();
        }

        if(disappearTimer >= disappearTime)
        {

            if(IsServer)
            {
                Destroy(gameObject);
            }
        }

        SuckMessIntoCollector();

        disappearTimer += startDisappearing ? Time.deltaTime : 0f;

        couldBePickedUpLastFrame = CanBePickedUp.Value;
    }

    /// <summary>
    /// Animate the object being pulled into the Roomba that collected it.
    /// </summary>
    void SuckMessIntoCollector()
    {
        if(startDisappearing || (alreadyPickedUp && collectorRoomba))
        {
            float progress = Mathf.InverseLerp(0f, disappearTime, disappearTimer);
            transform.position = Vector3.Slerp(SuckOrigin.Value, collectorRoomba.transform.position, progress * progress);
            if((transform.position - collectorRoomba.transform.position).sqrMagnitude >= MaxDistanceFromCollector*MaxDistanceFromCollector)
            {
                Vector3 dir = (collectorRoomba.transform.position - transform.position).normalized;
                transform.position = collectorRoomba.transform.position - dir * MaxDistanceFromCollector;
            }
            transform.localScale = Vector3.Slerp(initScale, FullSuck ? Vector3.zero : initScale / 2f, progress);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCollectorServerRpc(NetworkBehaviourReference collector, Vector3 suckOrigin, ServerRpcParams rpcParams = default)
    {
        Collector.Value = collector;
        AlreadyPickedUp.Value = true;
        SuckOrigin.Value = suckOrigin;

        SetCollectorClientRpc(collector, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds } });
    }

    [ClientRpc]
    public void SetCollectorClientRpc(NetworkBehaviourReference collector, ClientRpcParams rpcParams = default)
    {
        alreadyPickedUp = true;
        collector.TryGet(out collectorRoomba);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (IsServer && CanBePickedUp.Value)
        {
            RoombaControl roomba = other.GetComponent<RoombaControl>();

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

                SetCollectorServerRpc(new NetworkBehaviourReference(roomba), transform.position);
            }
        }
    }
}
