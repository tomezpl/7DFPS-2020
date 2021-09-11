using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic billboard script. Makes a sprite (rendered through a <see cref="MeshRenderer"/>) face towards the camera.
/// To allow for manually tweaking orientation, the <see cref="MeshRenderer"/> needs to be placed on a child object (to preserve manual orientation via localRotation).
/// </summary>
public class SimpleBillboard : MonoBehaviour
{
    protected Material billboardMaterial = null;
    protected Vector3 initPos = Vector3.zero;

    /// <summary>
    /// Offset to be applied towards the camera when it aligns with the billboard's X-axis.
    /// This is useful for things like ensuring the sprite doesn't clip into an object (cheating Z-ordering, basically).
    /// </summary>
    public float XAxisFacingOffset = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        billboardMaterial = GetComponentInChildren<MeshRenderer>().material;
        initPos = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        Camera currentCam = GameManager.Singleton?.SpawnedPlayer?.Cam ?? Camera.current ?? Camera.main;

        if(currentCam)
        {
            transform.LookAt(currentCam.transform, Vector3.up);

            ApplyLocalOffsetX(currentCam);

            UpdateShader();
        }
    }

    /// <summary>
    /// Brings the billboard closer to the <paramref name="currentCam"/> when the camera aligns with the parent's local X-axis.
    /// </summary>
    /// <param name="currentCam"></param>
    protected virtual void ApplyLocalOffsetX(Camera currentCam)
    {
        if (transform.parent)
        {
            float xAxisDot = Vector3.Dot(transform.parent.right, (transform.position - currentCam.transform.position).normalized);

            Vector3 unitOffset = transform.worldToLocalMatrix.MultiplyVector(transform.right) * transform.localScale.magnitude;
            transform.localPosition = initPos + unitOffset * XAxisFacingOffset * -xAxisDot;
        }
    }

    /// <summary>
    /// Override as needed for specific scripts & shaders.
    /// </summary>
    protected virtual void UpdateShader()
    {
        // dummy
    }
}
