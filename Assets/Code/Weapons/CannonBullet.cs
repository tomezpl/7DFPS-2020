using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonBullet : Despawnable
{
    public GameObject owner;
    public GameObject hit;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
    }

    private void OnCollisionEnter(Collision collision)
    {
        GetComponent<Rigidbody>().useGravity = true;
        if (collision.gameObject != owner && collision.transform.root != owner)
        {
            GetComponent<Rigidbody>().useGravity = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner.GetComponent<BoxCollider>() as Collider != other && hit == null)
        {
            hit = other.gameObject;
        }
    }
}
