using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CannonBullet : Despawnable
{
    /// <summary>
    /// Owner player object. Same as the Cannon's owner.
    /// </summary>
    public GameObject Owner;

    /// <summary>
    /// Hit player object (the victim).
    /// </summary>
    public GameObject Hit;

    /// <summary>
    /// Maximum damage to deal in a single shot.
    /// </summary>
    public int DamagePerShot = 40;

    /// <summary>
    /// Maximum distance at which <see cref="DamagePerShot"/> can be applied without penalty.
    /// </summary>
    public float DamageDropOff = 10f;
    
    /// <summary>
    /// Absolute minimum amount of damage to deal at a successful hit, regardless of distance.
    /// </summary>
    public int MinDamagePerShot = 1;

    /// <summary>
    /// The point where this bullet spawned.
    /// </summary>
    Vector3 spawnPosition;

    /// <summary>
    /// Distance this bullet travelled from spawn to hit.
    /// </summary>
    float distanceTravelled;

    /// <summary>
    /// Final amount of damage to deal on a hit. null if no hit happened yet.
    /// </summary>
    public float DamageDealt
    {
        get
        {
            return Hit != null ? Mathf.Lerp(MinDamagePerShot, (float)DamagePerShot, 1f - Mathf.InverseLerp(0f, DamageDropOff, distanceTravelled)) : 0f;
        }
    }

    /// <summary>
    /// Is the bullet currently resting on top of another collider?
    /// </summary>
    public bool IsResting = false;

    CannonBulletAudioController CannonBulletAudioController;

    // Start is called before the first frame update
    void Start()
    {
        // Store the initial position.
        spawnPosition = transform.position;
        CannonBulletAudioController = GetComponent<CannonBulletAudioController>();
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();

        if((transform.position - spawnPosition).magnitude > 20f)
        {
            //GetComponent<Rigidbody>().useGravity = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // The bullet doesn't use gravity by default to avoid bullet drop. Re-enable it once it hits something.
        GetComponent<Rigidbody>().useGravity = true;

        if (collision.gameObject != Owner && collision.transform.root != Owner && Hit == null)
        {
            distanceTravelled += Vector3.Distance(collision.GetContact(0).point, spawnPosition);
        }

        IsResting = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        IsResting = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        RoombaControl otherRoomba = other.transform.root.GetComponent<RoombaControl>();

        // Return immediately if the triggered Roomba is ours.
        if (otherRoomba?.OwnerClientId == Owner.GetComponent<NetworkObject>()?.OwnerClientId)
        {
            return;
        }
        // Perform hit detection only if the bullet hasn't hit anything already.
        else if(otherRoomba && Hit == null)
        {
            GetComponent<Rigidbody>().useGravity = true;
            distanceTravelled = Vector3.Distance(other.transform.position, spawnPosition);
            Hit = other.gameObject;

            if (Owner && Owner.GetComponent<Cannon>().Owner.PlayerControlled)
            {
                Debug.Log($"Attacking {other.name}");
                PlayerStats victimStats = otherRoomba.GetComponent<PlayerStats>();
                Owner.GetComponent<Cannon>().Owner.DealDamageServerRpc((int)Mathf.Round(DamageDealt), victimStats.OwnerClientId);
            }
        }
    }

    protected override bool CanDespawn()
    {
        return !(CannonBulletAudioController?.BulletCaseRollAudioSrc?.isPlaying == true) &&
            !(CannonBulletAudioController?.BulletHitRoombaAudioSrc?.isPlaying == true) &&
            base.CanDespawn();
    }
}
