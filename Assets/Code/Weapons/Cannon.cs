using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    public Camera cam;
    public Transform barrel;
    public RoombaControl owner;

    Quaternion _initRotation;

    // Start is called before the first frame update
    void Start()
    {
        // Store the intial orientation of the cannon, as per the prefab.
        _initRotation = transform.localRotation;

        if(!owner)
        {
            owner = transform.parent.GetComponent<RoombaControl>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        float camAngleY = Vector3.SignedAngle(owner.transform.forward, cam.transform.forward, cam.transform.up);
        transform.localRotation = _initRotation * Quaternion.AngleAxis(camAngleY, transform.up);
    }
}
