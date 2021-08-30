using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleBillboard : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Camera currentCam = Camera.current ?? Camera.main;

        if(currentCam)
        {
            //float pitchDiff = transform.position - currentCam.transform.position

            //transform.rotation = Quaternion.AngleAxis(-90f, Vector3.right) * Quaternion.AngleAxis(pitchDiff, Vector3.forward);

            transform.LookAt(currentCam.transform, Vector3.up);
            //transform.Rotate(transform.forward, 180f);
            //transform.Rotate(transform.right, -90f);
        }
    }
}
