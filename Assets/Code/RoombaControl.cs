using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoombaControl : MonoBehaviour
{
    public enum RoombaClass
    {
        Cannon = 0,
        Stabbo,
        Lithium
    }

    public RoombaClass selectedClass = RoombaClass.Cannon;

    public MeshCollider roombaCollider;

    public Camera cam;
    public float moveSpeed = 3f, strafeSpeed = 2f, jumpStrength = 5f;

    public Rigidbody rb;

    public bool playerControlled = true;

    public bool PlayerControlled { get { return playerControlled && GetComponent<PhotonView>().IsMine; } }

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

    float GetTurn() => Input.GetAxis("Horizontal");

    bool GetJump() => Input.GetButtonDown("Jump");

    void CameraLook()
    {
        // Turn the roomba left-right.
        transform.Rotate(transform.up, GetTurn() * Mathf.Sign(GetWalk()), Space.World);

        // Camera freelook
        float lookX = GetLookX();
        float lookY = GetLookY();
        if (cam.transform.localRotation.y > 0.3f)
        {
            lookX = lookX > 0f ? 0f : lookX;
        }
        if (cam.transform.localRotation.y < -0.3f)
        {
            lookX = lookX < 0f ? 0f : lookX;
        }

        if (cam.transform.localRotation.x > 0.4f)
        {
            lookY = lookY < 0f ? 0f : lookY;
        }
        if (cam.transform.localRotation.x < -0.4f)
        {
            lookY = lookY > 0f ? 0f : lookY;
        }

        cam.transform.Rotate(transform.up, lookX, Space.World);
        cam.transform.Rotate(cam.transform.right, -lookY, Space.World);
    }

    void Movement()
    {
        transform.Translate(MoveVector * GetWalk() * moveSpeed * Time.deltaTime, Space.World);

        // Allow jumping only if colliding with a floor.
        if (isColliding && GetJump())
        {
            // Jump with the current momentum.
            rb.AddForce((transform.up * jumpStrength) + (MoveVector * GetWalk() * moveSpeed), ForceMode.Impulse);
        }
    }

    [PunRPC]
    void SetWeapons(RoombaClass roombaClass)
    {
        selectedClass = roombaClass;
        switch(selectedClass)
        {
            case RoombaClass.Cannon:
                foreach(Weapon weapon in GetComponentsInChildren<Weapon>())
                {
                    if(!weapon.GetComponent<Cannon>())
                    {
                        weapon.gameObject.SetActive(false);
                    }
                }
                break;
            case RoombaClass.Stabbo:
                foreach(Weapon weapon in GetComponentsInChildren<Weapon>())
                {
                    if(!weapon.GetComponent<Knife>())
                    {
                        weapon.gameObject.SetActive(false);
                    }
                }
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        moveVector = transform.forward;
        strafeVector = transform.right;

        isColliding = false;

        if(!cam)
        {
            cam = GetComponentInChildren<Camera>();
        }
        if(!rb)
        {
            rb = GetComponent<Rigidbody>();
        }

        if(!PlayerControlled)
        {
            // If this isn't our roomba, disable the camera audio listener so Unity doesn't complain.
            cam.GetComponent<AudioListener>().enabled = false;

            // Prevent switching to the newly spawned roomba's camera by disabling it.
            cam.enabled = false;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerControlled)
        {
            CameraLook();
            Movement();

            if(Input.GetKeyDown(KeyCode.F4))
            {
                GetComponent<PlayerStats>().Die();
            }
        }
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
        if (collision.transform.CompareTag("Floor"))
        {
            isColliding = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Calculate walk & strafe vectors from floor surface tangents.
        moveVector = CalculateFloorMoveVector(collision);
        strafeVector = CalculateFloorStrafeVector(collision);

        if (collision.transform.CompareTag("Floor"))
        {
            isColliding = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.transform.CompareTag("Floor"))
        {
            isColliding = false;
        }
    }
}
