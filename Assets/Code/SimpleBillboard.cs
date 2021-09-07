using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleBillboard : MonoBehaviour
{
    Material billboardMaterial = null;

    // Start is called before the first frame update
    void Start()
    {
        billboardMaterial = GetComponentInChildren<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        Camera currentCam = GameManager.Singleton?.SpawnedPlayer?.Cam ?? Camera.current ?? Camera.main;

        if(currentCam)
        {
            //float pitchDiff = transform.position - currentCam.transform.position

            //transform.rotation = Quaternion.AngleAxis(-90f, Vector3.right) * Quaternion.AngleAxis(pitchDiff, Vector3.forward);

            transform.LookAt(currentCam.transform, Vector3.up);

            UpdateShader();
            //transform.Rotate(transform.forward, 180f);
            //transform.Rotate(transform.right, -90f);
        }
    }

    void UpdateShader()
    {
        if(billboardMaterial && transform.parent)
        {
            billboardMaterial.SetVector(Shader.PropertyToID("_MuzzleFlashDirection"), transform.parent.forward);
        }
    }
}
