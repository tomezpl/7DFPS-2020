using Photon.Pun;
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

    PhotonView photonView;

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
        photonView = PhotonView.Get(this);
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!photonView || !photonView.IsMine)
        {
            return;
        }

        GetComponent<Rigidbody>().useGravity = true;
        if (collision.gameObject != owner && collision.transform.root != owner && hit == null)
        {
            GetComponent<Rigidbody>().useGravity = true;
            _distanceTraveled += Vector3.Distance(collision.GetContact(0).point, _spawnPos);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!photonView || !photonView.IsMine)
        {
            return;
        }

        if (owner.GetComponent<BoxCollider>() as Collider != other && hit == null)
        {
            GetComponent<Rigidbody>().useGravity = true;
            _distanceTraveled += Vector3.Distance(other.transform.position, _spawnPos);
            hit = other.gameObject;
            hit.GetComponent<PlayerStats>().lastAttacker = owner.GetComponent<PlayerStats>();
        }
    }
}
