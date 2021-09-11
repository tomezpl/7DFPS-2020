using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void Start()
    {
        stackedRot = initOrientation = transform.localRotation;
    }

    public override void Trigger()
    {
        stackedRot = initOrientation * Quaternion.AngleAxis(TurnAngle, RotationAxis);
        stackedTime = TurnTime;
    }

    private void Update()
    {
        if (stackedTime > 0f)
        {
            transform.localRotation = Quaternion.Lerp(initOrientation, stackedRot, elapsedTime / stackedTime);

            elapsedTime += Time.deltaTime;
        }

        if(elapsedTime > stackedTime)
        {
            stackedRot = initOrientation;
            stackedTime = 0f;
            elapsedTime = 0f;

            transform.localRotation = initOrientation;
        }
    }
}
