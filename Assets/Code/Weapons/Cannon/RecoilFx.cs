using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A <see cref="FxController"/> that controls the Cannon's vertical recoil.
/// </summary>
public class RecoilFx : FxController
{
    /// <summary>
    /// Axis to perform recoil rotation on. Usually X-axis as the recoil will kick the gun back.
    /// </summary>
    public Vector3 PitchAxis = Vector3.right;

    /// <summary>
    /// Maximum amount of time a single recoil can take to reach <see cref="MaxRecoilPitch"/>.
    /// </summary>
    public float MaxRecoilLerpTime = 0.06f;

    /// <summary>
    /// Peak amount of recoil kick (in degrees).
    /// </summary>
    public float MaxRecoilPitch = -5f;

    /// <summary>
    /// Minimal amount of recoil kick (in degrees).
    /// </summary>
    public float MinRecoilPitch = -3f;

    /// <summary>
    /// <para>
    /// For use in external scripts: 
    /// this is the pitch <see cref="Quaternion"/> that can be multiplied with the gun's input-based rotation.
    /// </para>
    /// <para>
    /// Treat as this script's output value, needs to be applied in the right order because Quaternions.
    /// </para>
    /// </summary>
    public Quaternion RecoilPitchExternal = Quaternion.identity;

    /// <summary>
    /// Firing states to control animation behaviour.
    /// </summary>
    enum FiringState
    {
        Idle = 0,
        Relaxing,
        Firing
    }

    /// <summary>
    /// Current <see cref="FiringState"/> the weapon is in. Used to apply the right transformations.
    /// </summary>
    FiringState currentFiringState = FiringState.Idle;

    /// <summary>
    /// Pre-defined target quaternions for each <see cref="FiringState"/>.
    /// </summary>
    Quaternion[] targetQuaternions = new Quaternion[3];

    /// <summary>
    /// Time passed since start of current interpolation between target quaternions.
    /// </summary>
    float elapsedTime = 0f;

    public override void Trigger()
    {
        // Set current firing state as firing.
        currentFiringState = FiringState.Firing;
    }

    void Start()
    {
        // Initialise target quaternion offsets for each firing state.
        targetQuaternions[(int)FiringState.Idle] = Quaternion.identity;
        targetQuaternions[(int)FiringState.Relaxing] = Quaternion.AngleAxis(MinRecoilPitch, PitchAxis);
        targetQuaternions[(int)FiringState.Firing] = Quaternion.AngleAxis(MaxRecoilPitch, PitchAxis);
    }

    void Update()
    {
        // Reset firing state if maximum recoil has been reached.
        if(elapsedTime == MaxRecoilLerpTime)
        {
            currentFiringState = FiringState.Idle;
        }

        // Apply the recoil transformations based on the firing state.
        RecoilPitchExternal = Quaternion.Slerp(Quaternion.identity, targetQuaternions[(int)currentFiringState], elapsedTime / MaxRecoilLerpTime);

        elapsedTime += Time.deltaTime * (currentFiringState == FiringState.Firing ? 1f : -1f);
        elapsedTime = Mathf.Clamp(elapsedTime, 0f, MaxRecoilLerpTime);
    }
}
