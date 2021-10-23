using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knife : Weapon
{
    /// <summary>
    /// Duration of the stabbing animation.
    /// </summary>
    public float StabAnimDuration = 0.33f;

    /// <summary>
    /// The orientation (in euler angles) to rotate the hammer towards during the hit animation.
    /// </summary>
    public Vector3 HitAnimTargetEuler = new Vector3(90f, 0f, 0f);

    /// <summary>
    /// Damage dealt when the attacker is exactly behind the victim and at the same angle.
    /// </summary>
    public int DamagePerBackstab = 110;

    /// <summary>
    /// Owner player of this weapon.
    /// </summary>
    public PlayerStats Owner;

    /// <summary>
    /// The victim hit by this player's attack.
    /// </summary>
    public GameObject Hit;

    /// <summary>
    /// Timer to track the stabbing animation.
    /// </summary>
    float stabAnimTimer = 0f;

    /// <summary>
    /// Have we stabbed anyone during this attack event yet? Prevents damage being dealt on multiple frames.
    /// </summary>
    bool stabbedAlready = false;

    /// <summary>
    /// Initial position of the knife in the player object (for interpolating in the animation).
    /// </summary>
    Vector3 initLocalPosition;

    /// <summary>
    /// Initial rotation of the hammer in the player object (for interpolating in the animation).
    /// </summary>
    Quaternion initLocalOrientation;

    // Start is called before the first frame update
    void Start()
    {
        initLocalPosition = transform.localPosition;
        initLocalOrientation = transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Fire1") && stabAnimTimer <= 0f && IsMine)
        {
            StartStab();
            // Invoke RPC on server which will trigger the animation for other clients too.
            StartStabServerRpc();
        }

        StabAnimation();
    }

    /// <summary>
    /// Resets the stab animation parameters to the starting point.
    /// </summary>
    void StartStab()
    {
        // Start the timer when attack input is triggered.
        stabAnimTimer = StabAnimDuration;
        stabbedAlready = false;
    }

    /// <summary>
    /// Triggers a player's stab animation for all other clients.
    /// </summary>
    /// <param name="serverRpcParams"></param>
    [ServerRpc]
    void StartStabServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Number of target players (ie. excluding the stabbing player).
        int nbTargetPlayers = NetworkManager.Singleton.ConnectedClientsList.Count - 1;
        if (nbTargetPlayers >= 1)
        {
            ulong[] targetClients = new ulong[nbTargetPlayers];
            int counter = 0;
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClients.Keys)
            {
                if (clientId != serverRpcParams.Receive.SenderClientId)
                {
                    targetClients[counter++] = clientId;
                }
            }

            StartStabClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = targetClients } });
        }
    }

    /// <summary>
    /// Starts the animation of this player's stab.
    /// </summary>
    /// <param name="clientRpcParams"></param>
    [ClientRpc]
    void StartStabClientRpc(ClientRpcParams clientRpcParams = default) => StartStab();

    /// <summary>
    /// Perform the stabbing animation (just an interpolation of the knife's position).
    /// </summary>
    void StabAnimation()
    {
        if(stabAnimTimer <= 0f)
        {
            Hit = null;
            stabbedAlready = false;
            return;
        }

        float stabProgress = Mathf.InverseLerp(StabAnimDuration, StabAnimDuration * .5f, stabAnimTimer);
        float idleProgress = Mathf.InverseLerp(StabAnimDuration * .5f, 0f, stabAnimTimer);

        // Is the stab lunge complete now (and we're recovering to idle position)?
        bool stabbed = stabAnimTimer < StabAnimDuration * .5f;

        // Interpolate knife position.
        //transform.localPosition = Vector3.Lerp(initLocalPosition, initLocalPosition + Vector3.forward * .33f, stabbed ? 1f - idleProgress : stabProgress);

        // Interpolate hammer rotation.
        transform.localRotation = Quaternion.Slerp(initLocalOrientation, initLocalOrientation * Quaternion.Euler(HitAnimTargetEuler), stabbed ? 1f - idleProgress : stabProgress);

        // Update timer.
        stabAnimTimer -= Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        if(!IsMine)
        {
            return;
        }

        RoombaControl otherPlayer = other.GetComponent<RoombaControl>() ?? other.GetComponentInParent<RoombaControl>();

        // Check for stabs on enemy players.
        if(otherPlayer && !otherPlayer.PlayerControlled && stabAnimTimer > 0f && !stabbedAlready)
        {
            Hit = other.gameObject;
            stabbedAlready = true;

            // Calculate the damage for this stab. A perfect backstab should deal maximum damage (DamagePerBackstab).
            int stabDamage = Mathf.RoundToInt(DamagePerBackstab * Mathf.Max(0f, Vector3.Dot(Owner.GetComponent<RoombaControl>().transform.forward, otherPlayer.transform.forward)));

            Owner.GetComponent<RoombaControl>().DealDamageServerRpc(stabDamage, otherPlayer.OwnerClientId);
        }
    }
}
