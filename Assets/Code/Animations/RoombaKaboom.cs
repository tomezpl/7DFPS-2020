using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoombaKaboom : MonoBehaviour
{
    public FadeIn FadeIn;
    public ZoomIn ZoomIn;

    public RoombaControl RoombaControl;

    public GameObject[] ObjectsToHide;

    public bool RoombaHidden = false;

    public float HideRoombaAt = 0.2f;

    float timeElapsed = 0f;

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
            if(HideRoombaAt <= timeElapsed)
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
                timeElapsed += Time.deltaTime;
            }
        }
    }
}
