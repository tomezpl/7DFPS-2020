using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPPControl : MonoBehaviour
{
    public Camera camera;
    public float moveSpeed = 3f, strafeSpeed = 2f, jumpStrength = 3f;

    public Rigidbody rigidbody;

    // Movement vector; this is a cross product of the collider floor normal and the player's up vector. (Surface tangent)
    Vector3 moveVector;

    // Strafing vector
    Vector3 strafeVector;

    Vector3 MoveVector { get { return isColliding ? moveVector : Vector3.zero; } }

    Vector3 StrafeVector { get { return isColliding ? strafeVector : Vector3.zero; } }

    bool isColliding;

    float GetLookX() => Input.GetAxis("Mouse X");

    float GetLookY() => Input.GetAxis("Mouse Y");

    float GetWalk() => Input.GetAxis("Vertical");

    float GetStrafe() => Input.GetAxis("Horizontal");

    bool GetJump() => Input.GetButtonDown("Jump");

    void CameraLook()
    {
        float lookX = GetLookX();
        transform.Rotate(transform.up, lookX, Space.World);
        camera.transform.Rotate(transform.right, -GetLookY(), Space.World);
    }

    void Movement()
    {
        transform.Translate(MoveVector * GetWalk() * moveSpeed * Time.deltaTime, Space.World);
        transform.Translate(StrafeVector * GetStrafe() * strafeSpeed * Time.deltaTime, Space.World);

        if (isColliding && GetJump())
        {
            rigidbody.AddForce((transform.up * jumpStrength) + (MoveVector * GetWalk() * moveSpeed) + (StrafeVector * GetStrafe() * strafeSpeed), ForceMode.Impulse);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        moveVector = transform.forward;
        strafeVector = transform.right;

        isColliding = false;
    }

    // Update is called once per frame
    void Update()
    {
        CameraLook();
        Movement();
    }

    Vector3 CalculateSurfaceTangent(Vector3 surfaceNormal, Transform obj)
    {
        return Vector3.Cross(surfaceNormal, obj.transform.right).normalized;
    }

    Vector3 CalculateFloorMoveVector(Collision collision)
    {
        return -CalculateSurfaceTangent(collision.GetContact(0).normal, transform);
    }

    Vector3 CalculateFloorStrafeVector(Collision collision)
    {
        return -Vector3.Cross(CalculateFloorMoveVector(collision), transform.up);
    }

    private void OnCollisionEnter(Collision collision)
    {
        isColliding = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        moveVector = CalculateFloorMoveVector(collision);
        strafeVector = CalculateFloorStrafeVector(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        isColliding = false;
    }
}
