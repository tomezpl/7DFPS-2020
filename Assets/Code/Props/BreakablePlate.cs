using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Collectable prop that applies a fractured mask upon strong enough impact.
/// </summary>
public class BreakablePlate : CollectableMess
{
    public Texture FracturedAlphaTexture;
    public Renderer Renderer;
    public float SquaredImpulseMagnitudeToBreak = 10f;
    public NetworkVariable<bool> IsBroken = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone, false);

    private bool isBroken = false;
    private bool wasBrokenLastFrame = false;

    private Vector3 lastFrameVelocity = Vector3.zero, lastFrameVelocity2 = Vector3.zero;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        if(!Renderer)
        {
            Renderer = GetComponent<Renderer>();
        }

        if(Renderer)
        {
            if (!FracturedAlphaTexture)
            {
                FracturedAlphaTexture = Renderer.material.mainTexture;
            }
            Renderer.material.mainTexture = null;
        }

        isBroken = wasBrokenLastFrame = IsBroken.Value;

        if(isBroken)
        {
            ApplyFracturedVisuals();
        }

        CanBePickedUp.Value = couldBePickedUpLastFrame = false;
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();

        if(isBroken && !wasBrokenLastFrame)
        {
            ApplyFracturedVisuals();
        }

        wasBrokenLastFrame = isBroken;
    }

    public void FixedUpdate()
    {
        lastFrameVelocity2 = lastFrameVelocity;
        lastFrameVelocity = GetComponent<Rigidbody>().velocity;
    }

    [ClientRpc]
    public void BreakClientRpc(ClientRpcParams rpcParams = default)
    {
        isBroken = true;
        couldBePickedUpLastFrame = true;
    }

    [ServerRpc]
    public void BreakServerRpc(NetworkBehaviourReference roombaRef = default, bool pickUpInstantly = false, ServerRpcParams rpcParams = default)
    {
        IsBroken.Value = true;

        if(pickUpInstantly && roombaRef.TryGet(out RoombaControl roomba))
        {
            LobbyManager.Singleton.CurrentGameMode.EmitEvent(new CleanupJobEvents.MessCleanupEvent
            {
                CollectorId = roomba.OwnerClientId,
                MessRef = new NetworkBehaviourReference(this)
            });

            SetCollectorServerRpc(roombaRef, transform.position);
        }
        else
        {
            CanBePickedUp.Value = true;
        }

        BreakClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds } });
    }

    void ApplyFracturedVisuals()
    {
        if (Renderer && FracturedAlphaTexture)
        {
            Renderer.material.mainTexture = FracturedAlphaTexture;
        }

        if (TryGetComponent(out Collider collider))
        {
            collider.isTrigger = true;
        }

        if(TryGetComponent(out Rigidbody rigidbody))
        {
            rigidbody.isKinematic = true;
        }

        if(IsOwner)
        {
            if(Physics.Raycast(transform.position, Physics.gravity.normalized, out RaycastHit hit, 20f))
            {
                transform.rotation *= Quaternion.FromToRotation(transform.up, hit.normal);
                transform.position = hit.point;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsOwner)
        {
            if (Mathf.Max(lastFrameVelocity.sqrMagnitude, lastFrameVelocity2.sqrMagnitude) >= SquaredImpulseMagnitudeToBreak)
            {
                if (collision.gameObject.TryGetComponent(out RoombaControl roomba))
                {
                    BreakServerRpc(new NetworkBehaviourReference(roomba), true);
                }
                else
                {
                    BreakServerRpc(default);
                }
            }
        }
    }
}
