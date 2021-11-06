using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardInFrontOfCam : MonoBehaviour
{
    public bool KeepInFrontOfCamera = true;
    float initDistanceFromParent = 0f;

    void Start()
    {
        if (transform.parent)
        {
            initDistanceFromParent = (transform.position - transform.parent.position).magnitude;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Camera camera = GameManager.Singleton?.SpawnedPlayer?.Cam ?? Camera.main ?? Camera.current;

        // Slide towards parent/anchor position if billboard got behind camera.
        if (camera && transform.parent && KeepInFrontOfCamera)
        {
            float camDist = (camera.transform.position - transform.parent.position).magnitude;

            //Debug.Log($"{camDist}, {initDistanceFromParent}");

            transform.position = transform.parent.position + transform.up * Mathf.Min(camDist * 0.9f, initDistanceFromParent);
        }
    }
}
