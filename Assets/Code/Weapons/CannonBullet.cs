using System;
using System.Collections;
using System.Collections.Generic;
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

    // Start is called before the first frame update
    void Start()
    {
        // Store the initial position.
        spawnPosition = transform.position;
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // The bullet doesn't use gravity by default to avoid bullet drop. Re-enable it once it hits something.
        GetComponent<Rigidbody>().useGravity = true;

        if (collision.gameObject != Owner && collision.transform.root != Owner && Hit == null)
        {
            distanceTravelled += Vector3.Distance(collision.GetContact(0).point, spawnPosition);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        RoombaControl otherRoomba = other.transform.root.GetComponent<RoombaControl>();

        // Return immediately if the triggered Roomba is ours.
        if (otherRoomba?.IsOwner == true)
        {
            return;
        }
        // Perform hit detection only if the bullet hasn't hit anything already.
        else if(otherRoomba && Hit == null)
        {
            GetComponent<Rigidbody>().useGravity = true;
            distanceTravelled = Vector3.Distance(other.transform.position, spawnPosition);
            Hit = other.gameObject;

            if (Owner.GetComponent<RoombaControl>().PlayerControlled)
            {
                Debug.Log($"Attacking {other.name}");
                Hit.GetComponent<PlayerStats>().LastAttackerId.Value = Owner.GetComponent<PlayerStats>().OwnerClientId;
            }
        }
    }
}
