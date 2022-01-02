using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An <see cref="FxController"/> meant to rotate the bullet cylinder bone of the turret to act as a reload animation.
/// </summary>
public class BulletCylinderFx : FxController
{
    /// <summary>
    /// Angle (in degrees) to rotate the bullet cylinder by on each shot.
    /// </summary>
    public float TurnAngle = 45f;

    /// <summary>
    /// Time in seconds it should take to rotate the bullet cylinder by <see cref="TurnAngle"/>.
    /// </summary>
    public float TurnTime = 0.25f;

    /// <summary>
    /// Rotation axis for the bullet cylinder - helps with weird Blender exports.
    /// </summary>
    public Vector3 RotationAxis = Vector3.forward;

    /// <summary>
    /// Target rotation of the cylinder. "stacked" refers to the fact multiple shots can happen before the rotation completes.
    /// </summary>
    Quaternion stackedRot = Quaternion.identity;

    /// <summary>
    /// The time it should take to achieve <see cref="stackedRot"/>.
    /// </summary>
    float stackedTime = 0f;

    float elapsedTime = 0f;

    /// <summary>
    /// Initial orientation to start from.
    /// </summary>
    Quaternion initOrientation = Quaternion.identity;

    /// <summary>
    /// Is the cylinder currently rotating?
    /// </summary>
    public bool IsRotating { get => stackedTime > 0f; }

    private void Start()
    {
        stackedRot = initOrientation = transform.localRotation;
    }

    public override void Trigger()
    {
        // Queue a rotation to the next bullet in the cylinder.
        stackedRot = initOrientation * Quaternion.AngleAxis(TurnAngle, RotationAxis);
        stackedTime = TurnTime;
    }

    private void Update()
    {
        // Interpolate rotation over time.
        if (stackedTime > 0f)
        {
            transform.localRotation = Quaternion.Lerp(initOrientation, stackedRot, elapsedTime / stackedTime);

            elapsedTime += Time.deltaTime;
        }

        // Reset the values if a rotation finished.
        if(elapsedTime > stackedTime)
        {
            stackedRot = initOrientation;
            stackedTime = 0f;
            elapsedTime = 0f;

            transform.localRotation = initOrientation;
        }
    }
}
