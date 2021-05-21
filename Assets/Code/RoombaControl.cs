using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The main player script, handling movement and the player's overall physical presence in the scene.
/// </summary>
public class RoombaControl : NetworkBehaviour
{
    /// <summary>
    /// The player's position synchronised with the server.
    /// </summary>
    public NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    /// <summary>
    /// The player's class.
    /// </summary>
    public NetworkVariableInt SelectedClass = new NetworkVariableInt(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    /// <summary>
    /// <para>Indicates whether the player's class has already been activated.</para>
    /// <para>This notifies joining players that the local selectedClass variable should be read from SelectedClass NetworkVariable instead.</para>
    /// <para>TODO: This could potentially be removed if we just read straight from SelectedClass, or passed the class in NetworkStart. Needs to be investigated.</para>
    /// </summary>
    public NetworkVariableBool SelectedClassAlreadySet = new NetworkVariableBool(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    /// <summary>
    /// The player's orientation synced with the server.
    /// </summary>
    public NetworkVariableQuaternion Rotation = new NetworkVariableQuaternion(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    }, Quaternion.identity);

    /// <summary>
    /// The player's camera orientation (global) to replicate for other clients.
    /// </summary>
    public NetworkVariableQuaternion CameraRotation = new NetworkVariableQuaternion(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    /// <summary>
    /// Available player classes.
    /// </summary>
    public enum RoombaClass
    {
        Stabbo = 0,
        Cannon,
        Lithium
    }

    /// <summary>
    /// The class selected in the class selection menu.
    /// </summary>
    RoombaClass selectedClass = RoombaClass.Cannon;

    /// <summary>
    /// The <see cref="MeshCollider"/> used for the player.
    /// </summary>
    public MeshCollider RoombaCollider;

    /// <summary>
    /// Player camera.
    /// </summary>
    public Camera Cam;

    /// <summary>
    /// Player movement parameters.
    /// </summary>
    public float MoveSpeed = 3f, StrafeSpeed = 2f, JumpStrength = 5f;

    /// <summary>
    /// Player's rigidbody component for simulating physics.
    /// </summary>
    public Rigidbody Rigidbody;

    /// <summary>
    /// Should this be controlled by a human player?
    /// </summary>
    bool playerControlled = true;

    /// <summary>
    /// Should user input be ignored?
    /// </summary>
    public bool LockInput = false;

    /// <summary>
    /// Debugging only: should this player be considered the local player no matter what?
    /// </summary>
    public bool OverridePlayerControlCheck = false;

    /// <summary>
    /// Is this controlled by the local player?
    /// </summary>
    public bool PlayerControlled { get { return OverridePlayerControlCheck || (playerControlled && IsOwner); } }

    // Direct result of the movement vector calculation.
    Vector3 moveVector;

    // Direct result of the strafe vector calculation.
    Vector3 strafeVector;

    /// <summary>
    /// <para>Movement vector; this is a cross product of the collider floor normal and the player's up vector. (Surface tangent)</para>
    /// <para>Equal to <see cref="Vector3.zero"/> when the player collider is not touching a floor surface.</para>
    /// </summary>
    Vector3 MoveVector { get { return isColliding ? moveVector : Vector3.zero; } }

    /// <summary>
    /// Strafing vector (usually orthogonal to <see cref="MoveVector"/>)
    /// </summary>
    Vector3 StrafeVector { get { return isColliding ? strafeVector : Vector3.zero; } }

    /// <summary>
    /// <para>Is the player collider in contact with another collider?</para>
    /// <para>This gets updated in OnCollision/OnTrigger callbacks and should not be modified in the update loop.</para>
    /// </summary>
    bool isColliding;

    /// <summary>
    /// Look X axis getter.
    /// </summary>
    /// <returns></returns>
    float GetLookX() => Input.GetAxis("Mouse X");

    /// <summary>
    /// Look Y axis getter.
    /// </summary>
    /// <returns></returns>
    float GetLookY() => Input.GetAxis("Mouse Y");

    /// <summary>
    /// Walk input getter.
    /// </summary>
    /// <returns></returns>
    float GetWalk() => Input.GetAxis("Vertical");

    /// <summary>
    /// Yaw rotation input getter.
    /// </summary>
    /// <returns></returns>
    float GetTurn() => Input.GetAxis("Horizontal");

    /// <summary>
    /// Jump trigger input getter.
    /// </summary>
    /// <returns></returns>
    bool GetJump() => Input.GetButtonDown("Jump");

    /// <summary>
    /// Handle camera transformations.
    /// </summary>
    void CameraLook()
    {
        // Camera freelook
        float lookX = GetLookX();
        float lookY = GetLookY();

        // Limit camera yaw.
        if (Cam.transform.localRotation.y > 0.3f)
        {
            lookX = lookX > 0f ? 0f : lookX;
        }
        if (Cam.transform.localRotation.y < -0.3f)
        {
            lookX = lookX < 0f ? 0f : lookX;
        }

        // Limit camera pitch.
        if (Cam.transform.localRotation.x > 0.4f)
        {
            lookY = lookY < 0f ? 0f : lookY;
        }
        if (Cam.transform.localRotation.x < -0.4f)
        {
            lookY = lookY > 0f ? 0f : lookY;
        }

        // Apply limited camera rotations.
        Cam.transform.Rotate(transform.up, lookX, Space.World);
        Cam.transform.Rotate(Cam.transform.right, -lookY, Space.World);
    }

    /// <summary>
    /// Handles movement input on the local player.
    /// </summary>
    void Movement()
    {
        if(!IsOwner && !OverridePlayerControlCheck)
        {
            return;
        }

        // Turn the roomba left-right.
        transform.Rotate(transform.up, GetTurn() * Mathf.Sign(GetWalk()), Space.World);

        // Move the roomba forwards or backwards along the floor tangent depending on the input.
        transform.Translate(MoveVector * GetWalk() * MoveSpeed * Time.deltaTime, Space.World);

        // Allow jumping only if colliding with a floor.
        if (isColliding && GetJump())
        {
            // Jump with the current momentum.
            Rigidbody.AddForce((transform.up * JumpStrength) + (MoveVector * GetWalk() * MoveSpeed), ForceMode.Impulse);
        }

        SetTransformServerRpc(transform.position, transform.rotation);
    }

    /// <summary>
    /// <para>Update player transform on the server.</para>
    /// <para>While player movement is technically client-authoritative, this RPC can be used for any checks we may need.</para>
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="rpcParams"></param>
    [ServerRpc]
    void SetTransformServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        Position.Value = position;
        Rotation.Value = rotation;
    }

    /// <summary>
    /// Select the correct weapon loadout by disabling weapon objects other than the requested <see cref="RoombaClass"/>.
    /// </summary>
    void SetWeapons()
    {
        Debug.Log($"Setting {name}({OwnerClientId})'s RoombaClass to: {selectedClass}");
        switch(selectedClass)
        {
            case RoombaClass.Cannon:
                foreach(Weapon weapon in GetComponentsInChildren<Weapon>())
                {
                    Debug.Log("Enabling Cannon");
                    if (!weapon.GetComponent<Cannon>())
                    {
                        weapon.gameObject.SetActive(false);
                    }
                }
                break;
            case RoombaClass.Stabbo:
                foreach(Weapon weapon in GetComponentsInChildren<Weapon>())
                {
                    Debug.Log("Enabling stabbo");
                    if(!weapon.GetComponent<Knife>())
                    {
                        weapon.gameObject.SetActive(false);
                    }
                }
                break;
            case RoombaClass.Lithium:
                foreach (Weapon weapon in GetComponentsInChildren<Weapon>())
                {
                    Debug.Log("Enabling Phone");
                    if (!weapon.GetComponent<Phone>())
                    {
                        weapon.gameObject.SetActive(false);
                    }
                }
                break;
        }
    }

    [ServerRpc]
    void SetPlayerRoombaColourServerRpc(float r, float g, float b)
    {
        Color colour = new Color(r, g, b);
        RoombaCollider.GetComponent<Renderer>().material.color = colour;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialise movement basis vectors.
        moveVector = transform.forward;
        strafeVector = transform.right;

        // Initialise collision state.
        isColliding = false;

        // Find the camera object if not assigned.
        if(!Cam)
        {
            Cam = GetComponentInChildren<Camera>();
        }

        // Find the rigidbody component if not assigned.
        if(!Rigidbody)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        if(!PlayerControlled)
        {
            // If this isn't our roomba, disable the camera audio listener so Unity doesn't complain.
            Cam.GetComponent<AudioListener>().enabled = false;

            // Prevent switching to the newly spawned roomba's camera by disabling it.
            Cam.enabled = false;
        }
        else
        {
        }
    }

    /// <summary>
    /// Occurs before <see cref="Start"/>
    /// </summary>
    public override void NetworkStart()
    {
        if (PlayerControlled)
        {
            // Update our weapons on the server.
            SetWeaponsServerRpc((int)LobbyManager.Singleton.SelectedClass);

            // Assign this object as a reference in LobbyManager.
            LobbyManager.Singleton.LocalPlayerObject = gameObject;
            
            // Reset the spawn flag in GameManager.
            GameManager.Singleton.CanRequestSpawn = true;

            // Disable lobby UI and exit the pending-spawn state as we've already spawned.
            LobbyManager.Singleton.IsLobbyUiShown = false;
            LobbyManager.Singleton.DoesRequireSpawn = false;
        }
        else
        {
            // If the local player is joining, the existing players' classes are likely already synced,
            // so we can read the NetworkVariable and call SetWeapons already.
            if (SelectedClassAlreadySet.Value)
            {
                selectedClass = (RoombaClass)SelectedClass.Value;
                Debug.Log($"Enabling class loadout \"{selectedClass}\" for existing player {OwnerClientId}");
                SetWeapons();
            }
        }
    }

    /// <summary>
    /// Notifies server of a weapon class update, which will then trigger a client RPC on all clients to update it.
    /// </summary>
    /// <param name="rpcParams"></param>
    [ServerRpc]
    void SetWeaponsServerRpc(int requestedClass, ServerRpcParams rpcParams = default)
    {
        // Make sure to invoke the client RPC on all connected clients.
        ClientRpcParams clientRpcParams = new ClientRpcParams();
        clientRpcParams.Send.TargetClientIds = new ulong[NetworkManager.Singleton.ConnectedClients.Count];
        NetworkManager.Singleton.ConnectedClients.Keys.CopyTo(clientRpcParams.Send.TargetClientIds, 0);

        // Synchronise the selected class value.
        SelectedClass.Value = requestedClass;

        // Trigger SetWeapons on all connected clients.
        SetWeaponsClientRpc(requestedClass, clientRpcParams);

        // Mark that the class synchronisation is done.
        // That way, newly joining players can call SetWeapons on existing players' Roombas during NetworkStart.
        SelectedClassAlreadySet.Value = true;
    }

    /// <summary>
    /// Replicates this player's weapon class setup for other clients.
    /// </summary>
    /// <param name="requestedClass"></param>
    /// <param name="clientRpcParams"></param>
    [ClientRpc]
    void SetWeaponsClientRpc(int requestedClass, ClientRpcParams clientRpcParams = default)
    {
        // Update the local selected class.
        selectedClass = (RoombaClass)requestedClass;

        Debug.Log($"Updated {name}({OwnerClientId})'s weapon class to {selectedClass}");

        // Update game objects.
        SetWeapons();
    }

    /// <summary>
    /// Deals damage to a player.
    /// </summary>
    /// <param name="damageDealt">Amount of damage to deal.</param>
    /// <param name="victimClientId">Player to be damaged.</param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc]
    public void DealDamageServerRpc(int damageDealt, ulong victimClientId, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"DealDamage RPC came in from client {serverRpcParams.Receive.SenderClientId}");

        // Stats scripts for both victim & attacker.
        PlayerStats victimStats = null;
        PlayerStats attackerStats = null;

        // Find both stats scripts.
        foreach(PlayerStats stats in FindObjectsOfType<PlayerStats>())
        {
            if(stats.OwnerClientId == victimClientId)
            {
                victimStats = stats;
            }
            if(stats.OwnerClientId == serverRpcParams.Receive.SenderClientId)
            {
                attackerStats = stats;
            }
        }

        // If victim found in the scene, deal damage and sync it in the NetworkVariable.
        if (victimStats)
        {
            victimStats.Health.Value -= damageDealt;
        }
        else
        {
            Debug.LogWarning($"Couldn't deal damage to victim Player{OwnerClientId}!");
        }

        // If attacker found in the scene, assign them as the last attacker for that victim.
        if(victimStats && attackerStats)
        {
            Debug.Log($"Setting {GameManager.FromId(victimClientId).PlayerName.Value}'s LastAttackerId to {serverRpcParams.Receive.SenderClientId}");
            victimStats.LastAttacker.Value = $"{serverRpcParams.Receive.SenderClientId}";
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerControlled)
        {
            // Allow for preventing input using LockInput.
            // Otherwise run regular input-movement updates.
            if (!LockInput)
            {
                CameraLook();
                Movement();
            }

            // Update the camera orientation for other clients.
            CameraRotation.Value = Cam.transform.rotation;

            // Suicide key.
            if(Input.GetKeyDown(KeyCode.F4))
            {
                GetComponent<PlayerStats>().Die();
            }
        }
        else
        {
            // If this is not a local player, update the movement from the server.
            ReplicateServerMovement();
        }
    }

    /// <summary>
    /// <para>Updates the movement based on server-synced NetworkVariables.</para>
    /// <para>Can be used to do interpolation, prediction etc.</para>
    /// </summary>
    void ReplicateServerMovement()
    {
        transform.position = Position.Value;
        transform.rotation = Rotation.Value;

        // Also update the camera's independent orientation.
        // This is then used to align certain weapons, like the Cannon.
        Cam.transform.rotation = CameraRotation.Value;
    }

    /// <summary>
    /// Calculates the surface tangent relative to an object.
    /// </summary>
    /// <param name="surfaceNormal">Normal vector of a surface, usually retrieved from collision or raycast.</param>
    /// <returns></returns>
    Vector3 CalculateSurfaceTangent(Vector3 surfaceNormal)
    {
        return Vector3.Cross(surfaceNormal, transform.right).normalized;
    }

    /// <summary>
    /// Calculates movement vector of a surface, e.g. floor, that the player collides with.
    /// </summary>
    /// <param name="collision">Collision data.</param>
    /// <returns></returns>
    Vector3 CalculateFloorMoveVector(Collision collision)
    {
        return -CalculateSurfaceTangent(collision.GetContact(0).normal);
    }

    /// <summary>
    /// <para>Calculates strafe vector of a surface, e.g. floor, that the player collides with.</para>
    /// <para>Usually a cross product of <see cref="MoveVector"/> and local +Y axis.</para>
    /// </summary>
    /// <param name="collision">Collision data.</param>
    /// <returns></returns>
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
