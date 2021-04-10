using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonBullet : Despawnable
{
    public GameObject owner;
    public GameObject hit;
    public int damagePerShot = 40;
    public float damageDropOff = 10f;
    public int minDamagePerShot = 1;

    Vector3 _spawnPos;
    float _distanceTraveled;

    //PhotonView photonView;

    public float DamageDealt
    {
        get
        {
            return hit != null ? Mathf.Lerp(minDamagePerShot, (float)damagePerShot, 1f - Mathf.InverseLerp(0f, damageDropOff, _distanceTraveled)) : 0f;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _spawnPos = transform.position;
        //photonView = PhotonView.Get(this);
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
    }

    private void OnCollisionEnter(Collision collision)
    {
        /*if (!photonView || !photonView.IsMine)
        {
            return;
        }*/

        GetComponent<Rigidbody>().useGravity = true;
        if (collision.gameObject != owner && collision.transform.root != owner && hit == null)
        {
            // TODO: Shouldn't a hit be activated here? Don't want player hits being granted on shells that bounce off something!
            GetComponent<Rigidbody>().useGravity = true;
            _distanceTraveled += Vector3.Distance(collision.GetContact(0).point, _spawnPos);
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
        else if(otherRoomba && hit == null)
        {
            Debug.Log($"Attacking {other.name}");
            GetComponent<Rigidbody>().useGravity = true;
            _distanceTraveled = Vector3.Distance(other.transform.position, _spawnPos);
            hit = other.gameObject;
            hit.GetComponent<PlayerStats>().LastAttackerId.Value = owner.GetComponent<PlayerStats>().OwnerClientId;
        }

        /*if(!photonView || !photonView.IsMine)
        {
            return;
        }

        if (PhotonView.Get(other) && !PhotonView.Get(other).IsMine && hit == null)
        {
            Debug.Log($"Attacking {other}");
            GetComponent<Rigidbody>().useGravity = true;
            _distanceTraveled += Vector3.Distance(other.transform.position, _spawnPos);
            hit = other.gameObject;
            hit.GetComponent<PlayerStats>().lastAttacker = owner.GetComponent<PlayerStats>();
        }*/
    }
}
