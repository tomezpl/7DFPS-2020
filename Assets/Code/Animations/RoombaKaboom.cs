using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animation script for controlling the Roomba explosion effect.
/// </summary>
public class RoombaKaboom : MonoBehaviour
{
    public FadeIn FadeIn;
    public ZoomIn ZoomIn;

    public RoombaControl RoombaControl;

    /// <summary>
    /// Objects to hide when the timer reaches <see cref="HideRoombaAt"/>. These objects need to have Renderer components.
    /// </summary>
    public GameObject[] ObjectsToHide;

    /// <summary>
    /// Have <see cref="ObjectsToHide"/> been hidden?
    /// </summary>
    public bool RoombaHidden = false;

    /// <summary>
    /// Offset from <see cref="TimeElapsed"/> when <see cref="ObjectsToHide"/> should be made invisible.
    /// </summary>
    public float HideRoombaAt = 0.2f;

    public float TimeElapsed = 0f;

    // Start is called before the first frame update
    void Start()
    {
        RoombaControl = RoombaControl ?? GetComponentInParent<RoombaControl>();
        FadeIn = FadeIn ?? GetComponent<FadeIn>();
        ZoomIn = ZoomIn ?? GetComponent<ZoomIn>();
    }

    // Update is called once per frame
    void Update()
    {
        FadeIn.Started = RoombaControl.IsExploding;
        ZoomIn.Started = RoombaControl.IsExploding;

        if(!RoombaHidden && FadeIn.Started && ZoomIn.Started)
        {
            if(HideRoombaAt <= TimeElapsed)
            {
                RoombaHidden = true;

                foreach (GameObject roombaModule in ObjectsToHide)
                {
                    foreach (Renderer renderer in roombaModule.GetComponentsInChildren<Renderer>())
                    {
                        renderer.enabled = false;
                    }
                }
            }
            else
            {
                TimeElapsed += Time.deltaTime;
            }
        }
    }
}
