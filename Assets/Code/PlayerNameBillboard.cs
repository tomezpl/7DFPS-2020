using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Script to point all player overhead names to be facing towards the local player's camera.
/// </summary>
public class PlayerNameBillboard : MonoBehaviour
{
    MeshRenderer meshRenderer;
    RoombaControl owner;

    void Start()
    {
        RenderPipelineManager.beginCameraRendering += Reorient;
        meshRenderer = GetComponent<MeshRenderer>();
        owner = GetComponentInParent<RoombaControl>();
    }

    void Reorient(ScriptableRenderContext context, Camera camera)
    {
        RoombaControl renderingRoomba = camera.GetComponentInParent<RoombaControl>();

        if (renderingRoomba && renderingRoomba.PlayerGuid == owner.PlayerGuid)
        {
            meshRenderer.enabled = false;
        }
        else
        {
            meshRenderer.enabled = true;

            // Point the player name at the currently rendering camera.
            transform.LookAt(camera.transform);
            transform.Rotate(0f, 180f, 0f, Space.Self);
        }
    }
}
