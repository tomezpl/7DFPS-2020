using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeIn : MonoBehaviour
{
    float startOpacity = 1f;

    /// <summary>
    /// Time from start to end of animation (in seconds).
    /// </summary>
    public float FadeTime = 0.2f;

    public float StartFadingAt = 0.05f;

    float timeElapsed = 0f;

    bool finished = false;

    public bool Started = false;

    Renderer renderer;

    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<Renderer>();

        if (renderer)
        {
            startOpacity = renderer.material.color.a;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!Started)
        {
            return;
        }

        if (timeElapsed - StartFadingAt >= FadeTime)
        {
            finished = true;
        }

        if (!finished && renderer)
        {
            Color startColor = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, startOpacity);
            Color endColor = startColor;
            endColor.a = 0f;
            renderer.material.color = Color.Lerp(startColor, endColor, FadeTime == 0f ? 1f : Mathf.Max(0, timeElapsed - StartFadingAt) / FadeTime);

            timeElapsed += Time.deltaTime;
        }
    }
}
