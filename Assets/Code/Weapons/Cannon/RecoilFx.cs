using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecoilFx : FxController
{
    public Vector3 PitchAxis = Vector3.right;

    public float MaxRecoilLerpTime = 0.06f;

    public float MaxRecoilPitch = -5f;
    public float MinRecoilPitch = -3f;

    float minRecoilPitchLerp = 0f;

    /// <summary>
    /// For use in external scripts: 
    /// this is the pitch quaternion that can be multiplied with the gun's input-based rotation.
    /// </summary>
    public Quaternion RecoilPitchExternal = Quaternion.identity;

    Quaternion targetQuaternion = Quaternion.identity;

    enum FiringState
    {
        Idle = 0,
        Relaxing,
        Firing
    }

    FiringState currentFiringState = FiringState.Idle;
    Quaternion[] targetQuaternions = new Quaternion[3];

    float elapsedTime = 0f;

    public override void Trigger()
    {
        currentFiringState = FiringState.Firing;

        targetQuaternion = targetQuaternions[(int)currentFiringState];
    }

    void Start()
    {
        if (MaxRecoilPitch != 0f)
        {
            minRecoilPitchLerp = MinRecoilPitch / MaxRecoilPitch;
        }

        targetQuaternions[(int)FiringState.Idle] = Quaternion.identity;
        targetQuaternions[(int)FiringState.Relaxing] = Quaternion.AngleAxis(MinRecoilPitch, PitchAxis);
        targetQuaternions[(int)FiringState.Firing] = Quaternion.AngleAxis(MaxRecoilPitch, PitchAxis);
    }

    void Update()
    {
        if(elapsedTime == MaxRecoilLerpTime)
        {
            currentFiringState = FiringState.Idle;
        }

            RecoilPitchExternal = Quaternion.Slerp(Quaternion.identity, targetQuaternions[(int)currentFiringState], elapsedTime / MaxRecoilLerpTime);

        elapsedTime += Time.deltaTime * (currentFiringState == FiringState.Firing ? 1f : -1f);
        elapsedTime = Mathf.Clamp(elapsedTime, 0f, MaxRecoilLerpTime);
    }
}
