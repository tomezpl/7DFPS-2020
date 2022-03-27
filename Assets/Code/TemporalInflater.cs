using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemporalInflater : MonoBehaviour
{
    public Func<float> ApplyFunc;

    public float CurrentValue { get => Mathf.Lerp(0f, TargetValue, Mathf.InverseLerp(0f, EndTime, TimeElapsed)); }

    public float TimeElapsed = 0f;

    public float EndTime = 0f;

    public float TargetValue = 0f;

    bool reached;

    // Start is called before the first frame update
    void Start()
    {
        TimeElapsed = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        ApplyFunc();

        if (TimeElapsed < EndTime)
        {
            TimeElapsed += Time.deltaTime;
        }
        else if(!reached)
        {
            reached = true;

            Destroy(gameObject);
        }
    }
}
