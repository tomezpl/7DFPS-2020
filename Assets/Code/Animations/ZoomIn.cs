using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomIn : MonoBehaviour
{
    Vector3 targetScale = Vector3.one;

    /// <summary>
    /// Time from start to end of animation (in seconds).
    /// </summary>
    public float ZoomTime = 0.2f;

    float timeElapsed = 0f;

    bool finished = false;

    public bool Started = false;

    // Start is called before the first frame update
    void Start()
    {
        targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Started)
        {
            return;
        }

        if(timeElapsed >= ZoomTime)
        {
            finished = true;
        }

        if (!finished)
        {
            transform.localScale = Vector3.Slerp(Vector3.zero, targetScale, ZoomTime == 0f ? 1f : timeElapsed / ZoomTime);

            timeElapsed += Time.deltaTime;
        }
    }
}
