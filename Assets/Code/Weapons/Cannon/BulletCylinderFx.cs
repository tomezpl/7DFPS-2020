using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletCylinderFx : FxController
{
    public float TurnAngle = 45f;

    public float TurnTime = 0.25f;

    public Vector3 RotationAxis = Vector3.forward;

    Quaternion stackedRot = Quaternion.identity;
    float stackedTime = 0f;
    float elapsedTime = 0f;

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
