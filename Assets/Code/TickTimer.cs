using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Counts time and invokes <see cref="ApplyFunc"/> on every update, then destroys once target time has been reached.
/// </summary>
public class TickTimer : MonoBehaviour
{
    public Action ApplyFunc;

    public float CurrentValue { get => Mathf.Lerp(0f, TargetValue, Mathf.InverseLerp(0f, EndTime, TimeElapsed)); }

    public float TimeElapsed = 0f;

    public float EndTime = 0f;

    public float TargetValue = 0f;

    public bool Reached;

    // Start is called before the first frame update
    void Start()
    {
        TimeElapsed = 0f;
        Reached = false;
    }

    // Update is called once per frame
    void Update()
    {
        ApplyFunc();

        if (TimeElapsed < EndTime)
        {
            TimeElapsed += Time.deltaTime;
        }
        else if(!Reached)
        {
            Reached = true;

            Destroy(gameObject);
        }
    }
}
