using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDirectionArrow : MonoBehaviour
{
    Vector3 initialPosition;
    Vector3 initialScale;

    /// <summary>
    /// Maximum vertical offset (in local space) for the indicator.
    /// </summary>
    public float MaxYOffset = 0.15f;

    public Transform PlayerCamera;

    MeshRenderer renderer;

    RoombaControl owner;

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;

        renderer = GetComponent<MeshRenderer>();
        owner = GetComponentInParent<RoombaControl>();
    }

    // Update is called once per frame
    void Update()
    {
        SlideIndicator();

        if (owner && !owner.PlayerControlled)
        {
            renderer.enabled = false;
        }
        else
        {
            renderer.enabled = true;
        }
    }

    /// <summary>
    /// Slides the directional indicator up and down based on the viewing direction to make sure it's always in peripheral vision.
    /// </summary>
    void SlideIndicator()
    {
        // The camera's position on the local XZ plane.
        Vector3 cameraDir = PlayerCamera.transform.forward;

        // The indicator pointer's direction.
        Vector3 indicatorDir = transform.forward;

        float direction = Vector3.Dot(indicatorDir, cameraDir) - 0.66f;
        direction = Mathf.Max(0f, direction) * (1f / 0.66f);

        float maxCamDist = owner.InitialCameraOffset.magnitude;

        float t = 1f - Mathf.InverseLerp(owner.MinCameraDistance, maxCamDist, (PlayerCamera.transform.position - owner.transform.position).magnitude);
        float yPos = Mathf.Lerp(0.25f, -0.25f, Mathf.InverseLerp(-maxCamDist, maxCamDist, PlayerCamera.transform.localPosition.y));

        transform.localPosition = Vector3.Lerp(initialPosition, initialPosition + Vector3.up * MaxYOffset * (0.7f + yPos) - Vector3.forward * MaxYOffset * 1f, t);
        transform.localScale = Vector3.Lerp(initialScale, initialScale * Mathf.Lerp(0.4f, 0.85f, 1f - direction * 2f), direction + t / 2f);
    }
}
