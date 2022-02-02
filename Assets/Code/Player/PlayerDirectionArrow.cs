using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    GameObject ownerGameObject;

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;

        renderer = GetComponent<MeshRenderer>();
        owner = GetComponentInParent<RoombaControl>();

        RenderPipelineManager.beginCameraRendering += UpdateArrowRenderer;
    }


    void UpdateArrowRenderer(ScriptableRenderContext context, Camera camera)
    {
        if (owner != camera.GetComponentInParent<RoombaControl>())
        {
            renderer.enabled = false;
        }
        else
        {
            renderer.enabled = true;
            SlideIndicator();
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

        // Scalar used to determine whether we're looking back (>0f) and how much.
        // This will be used to scale the player direction indicator down to indicate change in camera direction.
        float direction = Vector3.Dot(indicatorDir, cameraDir) - 0.66f;
        direction = Mathf.Max(0f, direction) * (1f / 0.66f);

        // Maximum distance the the camera can be from the player.
        float maxCamDist = owner.InitialCameraOffset.magnitude;

        // Interpolant for scaling/moving the indicator away from camera based on camera distance from player.
        float t = 1f - Mathf.InverseLerp(owner.MinCameraDistance, maxCamDist, (PlayerCamera.transform.position - owner.transform.position).magnitude);

        // Scalar for determining whether Y-offset should be applied with positive/negative sign,
        // based on whether the camera is looking from above or from below.
        float yPos = Mathf.Lerp(0.25f, -0.25f, Mathf.InverseLerp(-maxCamDist, maxCamDist, PlayerCamera.transform.localPosition.y));

        transform.localPosition = Vector3.Lerp(initialPosition, initialPosition + Vector3.up * MaxYOffset * (0.7f + yPos) - Vector3.forward * MaxYOffset * 1f, t);
        transform.localScale = Vector3.Lerp(initialScale, initialScale * Mathf.Lerp(0.4f, 0.85f, 1f - direction * 2f), direction + t / 2f);
    }
}
