using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// The main player script, handling movement and the player's overall physical presence in the scene.
/// </summary>
public class RoombaControl : NetworkBehaviour
{
    /// <summary>
    /// The player's position synchronised with the server.
    /// </summary>
    public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>(NetworkVariableReadPermission.Everyone, Vector3.zero);

    /// <summary>
    /// The player's class.
    /// </summary>
    public NetworkVariable<int> SelectedClass = new NetworkVariable<int>(NetworkVariableReadPermission.Everyone, 0);

    /// <summary>
    /// <para>Indicates whether the player's class has already been activated.</para>
    /// <para>This notifies joining players that the local selectedClass variable should be read from SelectedClass NetworkVariable instead.</para>
    /// <para>TODO: This could potentially be removed if we just read straight from SelectedClass, or passed the class in NetworkStart. Needs to be investigated.</para>
    /// </summary>
    public NetworkVariable<bool> SelectedClassAlreadySet = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone, false);

    /// <summary>
    /// The player's orientation synced with the server.
    /// </summary>
    public NetworkVariable<Quaternion> Rotation = new NetworkVariable<Quaternion>(NetworkVariableReadPermission.Everyone, Quaternion.identity);

    /// <summary>
    /// The player's camera orientation (global) to replicate for other clients.
    /// </summary>
    public NetworkVariable<Quaternion> CameraRotation = new NetworkVariable<Quaternion>(NetworkVariableReadPermission.Everyone, Quaternion.identity);

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
    /// The <see cref="Collider"/> used for the player.
    /// </summary>
    public Collider RoombaCollider;

    /// <summary>
    /// Player camera.
    /// </summary>
    public Camera Cam;

    /// <summary>
    /// The vertical offset to add to the camera's third-person position. Can be tweaked to give better vision while aiming.
    /// </summary>
    public float CamVerticalOffset = 1.1f;

    /// <summary>
    /// Player movement parameters.
    /// </summary>
    public float MoveSpeed = 3f, StrafeSpeed = 2f, TurnSpeed = 150f, JumpStrength = 5f;

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

    // Direct result of the movement vector calculation. moveVector2: raycast (will take priority)
    Vector3 moveVector, moveVector2;

    // Direct result of the strafe vector calculation. strafeVector2: raycast (will take priority)
    Vector3 strafeVector, strafeVector2;

    /// <summary>
    /// <para>Movement vector; this is a cross product of the collider floor normal and the player's up vector. (Surface tangent)</para>
    /// <para>Equal to <see cref="Vector3.zero"/> when the player collider is not touching a floor surface.</para>
    /// </summary>
    Vector3 MoveVector { get { return isColliding ? (moveVector2 == Vector3.zero ? moveVector : moveVector2) : Vector3.zero; } }

    /// <summary>
    /// Strafing vector (usually orthogonal to <see cref="MoveVector"/>)
    /// </summary>
    Vector3 StrafeVector { get { return isColliding ? (strafeVector2 == Vector3.zero ? strafeVector : strafeVector2) : Vector3.zero; } }

    /// <summary>
    /// <para>Is the player collider in contact with another collider?</para>
    /// <para>This gets updated in OnCollision/OnTrigger callbacks and should not be modified in the update loop.</para>
    /// </summary>
    bool isColliding;

    /// <summary>
    /// Should the camera be rigidly aligned with the player's rotation?
    /// </summary>
    public bool OrbitCameraWithPlayer = false;

    /// <summary>
    /// The time (in seconds) of input inactivity it takes for the camera to start resetting to its initial transform.
    /// </summary>
    public float CameraIdleTimeout = 5f;

    /// <summary>
    /// Should the player's camera reset after <see cref="CameraIdleTimeout"/> is reached?
    /// </summary>
    public bool IsCameraIdleTimeoutEnabled = false;

    /// <summary>
    /// Timer for tracking camera input inactivity. If it reaches <see cref="CameraIdleTimeout"/>, the camera begins to reset to its initial position.
    /// </summary>
    float camIdleTimer = 0f;

    /// <summary>
    /// Timer for tracking elapsed time during the camera reset interpolation.
    /// </summary>
    float camResetProgress = 0f;

    /// <summary>
    /// The amount of smoothing to apply to the camera reset interpolation (0..1)
    /// </summary>
    public float CameraResetSmoothing = 1f;

    /// <summary>
    /// The time (in seconds) it should take to reset back to the initial camera tranform.
    /// </summary>
    public float CameraResetTime = 3f;

    /// <summary>
    /// The minimum distance the camera should keep from the origin point.
    /// </summary>
    public float MinCameraDistance = 1.5f;

    /// <summary>
    /// The distance to add to the springarm camera raycast max distance.
    /// </summary>
    public float SpringarmCameraRaycastMargin = 0.2f;

    /// <summary>
    /// <para>The maximum number of raycast hits we should check for the springarm camera.</para>
    /// <para>This can be tuned for minor performance tweaking as it adjusts the number of for-loop iterations.</para>
    /// </summary>
    public int MaxCameraRaycastIterations = 10;

    /// <summary>
    /// Initial camera local position - this will be set during <see cref="Start"/>
    /// </summary>
    public Vector3 InitialCameraOffset = Vector3.zero;

    /// <summary>
    /// Initial camera local orientation - this will be set during <see cref="Start"/>
    /// </summary>
    Quaternion initialCameraOrientation = Quaternion.identity;

    /// <summary>
    /// Camera local orientation right before <see cref="CameraIdleTimeout"/> was reached.
    /// </summary>
    Quaternion cameraOrientationBeforeResetY, cameraOrientationBeforeResetX = Quaternion.identity;

    /// <summary>
    /// <para>Is the camera currently being reset to its initial position?</para>
    /// <para>This will be true if at least <see cref="CameraIdleTimeout"/> has passed without camera input.</para>
    /// <para>As soon as input is received again, it will be set back to false, thus interrupting the reset and giving player camera control.</para>
    /// </summary>
    bool isCameraResetting = false;

    /// <summary>
    /// Used for raycasts to check if there's a wall blocking the Roomba's path.
    /// </summary>
    bool canMoveAhead = true;

    /// <summary>
    /// The radius of the explosion point light(s).
    /// </summary>
    public float ExplosionLightRadius = 1.5f;

    /// <summary>
    /// The explosion point light(s).
    /// </summary>
    public Light[] ExplosionLights;

    /// <summary>
    /// Explosion point lights' base intensities.
    /// </summary>
    public float[] ExplosionLightIntensities;

    /// <summary>
    /// The duration of the explosion effect (in seconds).
    /// </summary>
    public float ExplosionFxTime = 1f;

    /// <summary>
    /// Is the phone currently in process of detonation?
    /// </summary>
    public bool IsExploding = false;

    /// <summary>
    /// Timer that will count until <see cref="ExplosionFxTime"/>.
    /// </summary>
    public float ExplosionFxTimer = 0f;

    /// <summary>
    /// Should the crosshair be shown?
    /// </summary>
    public bool ShowCrosshair = true;

    #region Fix for camera turning when player moves sideways against a wall
    /// <summary>
    /// Position on the last fixed frame.
    /// </summary>
    Vector3 prevPosition = Vector3.zero;

    /// <summary>
    /// Velocity vector calculated explicitly on FixedUpdate using <see cref="prevPosition"/>.
    /// </summary>
    Vector3 explicitVelocity = Vector3.zero;

    /// <summary>
    /// Forward vector on last fixed frame.
    /// </summary>
    Vector3 fixedPrevForward = Vector3.forward;

    /// <summary>
    /// Is the player moving sideways (e.g. due to turning while also driving into a wall)? Determined on fixed physics step.
    /// </summary>
    bool isSliding = false;

    /// <summary>
    /// Is the player moving forwards?
    /// </summary>
    bool isDriving = false;

    /// <summary>
    /// Actual determinant used for <see cref="isSliding"/>.
    /// </summary>
    float slidingDot = 1f;

    /// <summary>
    /// An interpolant value [0..1] of how much of the threshold sideways velocity we have.
    /// </summary>
    float isSlidingInterpolant = 1f;
    #endregion

    /// <summary>
    /// Whether the crosshair should be rendered or not. Provide this to the SRP pass.
    /// </summary>
    public static bool CrosshairRequired
    {
        get
        {
            if(LobbyManager.Singleton?.LocalPlayerObject)
            {
                return LobbyManager.Singleton.LocalPlayerObject.GetComponent<RoombaControl>().selectedClass == RoombaClass.Cannon;
            }
            else
            {
                return FindObjectOfType<RoombaControl>()?.ShowCrosshair ?? false;
            }
        }
    }

    public RoombaAudioController RoombaAudioController;

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
    /// <param name="raw">Should the raw axis value be returned? (no gravity, sensitivity etc.)</param>
    /// <returns></returns>
    public float GetWalk(bool raw = false) => raw ? Input.GetAxisRaw("Forward") + Input.GetAxisRaw("Backward") : Input.GetAxis("Forward") + Input.GetAxis("Backward");

    /// <summary>
    /// Yaw rotation input getter.
    /// </summary>
    /// <param name="raw">Should the raw axis value be returned? (no gravity, sensitivity etc.)</param>
    /// <returns></returns>
    float GetTurn(bool raw = false) => raw ? Input.GetAxisRaw("Horizontal") : Input.GetAxis("Horizontal");

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

        if (lookX == 0f && lookY == 0f)
        {
            camIdleTimer += Time.deltaTime;
        }
        else
        {
            camIdleTimer = 0f;
            camResetProgress = 0f;

            isCameraResetting = false;
        }

        if (isCameraResetting)
        {
            camResetProgress += Time.deltaTime;
        }

        if (camIdleTimer >= CameraIdleTimeout && IsCameraIdleTimeoutEnabled)
        {
            if (!isCameraResetting)
            {
                // Set the start value for the interpolation.
                Vector3 euler = Cam.transform.localRotation.eulerAngles;

                cameraOrientationBeforeResetY = Quaternion.AngleAxis(euler.y, Vector3.up);
                cameraOrientationBeforeResetX = Quaternion.AngleAxis(euler.x, Vector3.right);
            }

            isCameraResetting = true;
        }

        // Limit camera pitch unless the camera is being reset.
        if (!isCameraResetting)
        {
            Vector3 eulers = Cam.transform.localRotation.eulerAngles;
            float pitch = Mathf.Cos(Mathf.Deg2Rad * eulers.x);
            if (eulers.x > 180f)
            {
                if (lookY > 0f)
                {
                    if (pitch <= 0.75f)
                    {
                        lookY = -lookY;
                    }
                }
            }
            if (eulers.x < 180f)
            {
                if (lookY < 0f)
                {
                    if (pitch <= 0.75f)
                    {
                        lookY = -lookY;
                    }
                }
            }
            //Debug.Log(pitch);
        }

        // Apply limited camera rotations.
        if (!isCameraResetting)
        {
            Cam.transform.Rotate(transform.up, lookX, Space.World);
            Cam.transform.Rotate(Cam.transform.right, -lookY, Space.World);
        }
        else
        {
            float expectedInterpolant = Mathf.InverseLerp(0f, Mathf.Max(CameraResetTime, camResetProgress), camResetProgress);

            // The percentile of the camera reset interpolation at which it should move at linear speed.
            // This is when smoothing disappears and interpolation goes full speed.
            float linearInterpolant = CameraResetSmoothing / 2f;
            float smoothInterpolantR = 1f - linearInterpolant;
            float finalInterpolant = 0f;
            if (expectedInterpolant < linearInterpolant && camResetProgress < CameraResetTime)
            {
                finalInterpolant = expectedInterpolant * Mathf.Max(0.01f, Mathf.InverseLerp(0f, linearInterpolant, expectedInterpolant));
            }
            else
            {
                finalInterpolant = expectedInterpolant;
            }

            // TODO: extract yaw and pitch using euler? might be easier & more confident in that
            initialCameraOrientation.ToAngleAxis(out float initAngle, out Vector3 initAxis);
            Quaternion initYaw = Quaternion.AngleAxis(initAngle * Vector3.Dot(initAxis, Vector3.up), Vector3.up);
            Quaternion initPitch = Quaternion.AngleAxis(initAngle * Vector3.Dot(initAxis, Vector3.right), Vector3.right);

            Quaternion yawLerp = Quaternion.Slerp(cameraOrientationBeforeResetY, initYaw, finalInterpolant);

            Quaternion pitchLerp = Quaternion.Slerp(cameraOrientationBeforeResetX, initPitch, finalInterpolant);

            Cam.transform.localRotation = yawLerp * pitchLerp;
        }

        Vector3 localEuler = Cam.transform.localEulerAngles * Mathf.Deg2Rad;
        float cameraDistance = InitialCameraOffset.magnitude;

        // Apply orbit offset.
        Cam.transform.localPosition = cameraDistance * new Vector3(-Mathf.Sin(localEuler.y) * Mathf.Cos(localEuler.x), Mathf.Sin(localEuler.x), -Mathf.Cos(localEuler.y) * Mathf.Cos(localEuler.x));
        Cam.transform.localPosition += Vector3.up * CamVerticalOffset;

        // Springarm camera: prevent objects from obstructing the player from the camera.
        float camRaycastHitDistance = cameraDistance;
        RaycastHit[] raycastResults = Physics.RaycastAll(transform.position, Cam.transform.position - transform.position, cameraDistance + SpringarmCameraRaycastMargin, ~LayerMask.GetMask("LocalPlayer", "SmallProjectile", "Ignore Raycast"));
        RaycastHit closestHit = default;
        for (int i = 0; i < raycastResults?.Length && i <= MaxCameraRaycastIterations; i++)
        {
            RaycastHit hit = raycastResults[i];
            camRaycastHitDistance = Mathf.Min(camRaycastHitDistance, hit.distance);

            if (camRaycastHitDistance == hit.distance)
            {
                closestHit = hit;
            }
        }

        Cam.transform.localPosition = Cam.transform.localPosition.normalized * Mathf.Max(MinCameraDistance, Mathf.Min(camRaycastHitDistance, cameraDistance));

        // Slide the camera along the surface.
        if (camRaycastHitDistance < cameraDistance && raycastResults?.Length > 0)
        {
            // n3
            Vector3 hitToCam = Cam.transform.position - closestHit.point;

            // n2
            Vector3 hitToNormal = closestHit.normal.normalized * hitToCam.magnitude;

            // Position of the camera along the surface tangent.
            Vector3 camSurfaceTangentPos = closestHit.point + (hitToCam + hitToNormal) / 2f;

            // Make sure the camera would be at least a bare minimum away 
            // from the player model so we don't see the insides (ewww).
            float newDist = Vector3.Distance(camSurfaceTangentPos, Cam.transform.position);
            if (camRaycastHitDistance < MinCameraDistance)
            {
                Vector3 localCamPos = transform.worldToLocalMatrix.MultiplyPoint(camSurfaceTangentPos);
                Cam.transform.localPosition = localCamPos.normalized * Mathf.Max(localCamPos.magnitude, MinCameraDistance / 1.25f);
            }
        }

        //Debug.DrawLine(Cam.transform.position, Cam.transform.position + Cam.transform.forward * cameraDistance, Color.red, 10f);
    }

    /// <summary>
    /// Handles movement input on the local player.
    /// </summary>
    void Movement()
    {
        if (!IsOwner && !OverridePlayerControlCheck)
        {
            return;
        }

        float roombaRotation = GetTurn() * Mathf.Sign(GetWalk()) * Time.deltaTime * TurnSpeed;

        // Turn the roomba left-right.
        Quaternion prevCamRotation = Cam.transform.rotation;
        Vector3 prevCamPosition = Cam.transform.position;
        transform.Rotate(transform.up, roombaRotation, Space.World);

        if (!OrbitCameraWithPlayer)
        {
            // Prevents camera turning around the player when they're stuck against a wall.
            bool turningInputActive = Mathf.Abs(GetTurn(true)) > 0f;
            bool drivingInputActive = Mathf.Abs(GetWalk(true)) > 0f;

            // Use the dot product to find how much our current velocity is aligned with the forward-vector.
            float forwardDot = Mathf.Abs(Vector3.Dot(explicitVelocity.normalized, transform.forward));

            // Determine if we're successfully moving forwards or sliding sideways as a result of pushing against a wall.
            isDriving = forwardDot > 0.9f;
            isSliding = slidingDot > forwardDot;

            // If all of those conditions are true, the camera's global transform will be reset to that of last frame.
            bool preventCameraTurn = turningInputActive && drivingInputActive && !isDriving && isSliding;

            // Counters the player rotation.
            Cam.transform.rotation = (preventCameraTurn) ? Cam.transform.rotation : prevCamRotation;

            // Prevents jitter.
            Cam.transform.position = (preventCameraTurn) ? Cam.transform.position : prevCamPosition;
        }

        // Raycast in front of the player to check for inclines/slopes/obstacles.
        CheckAhead();

        // Perform predictive pitch rotation to align with the raycast-hit surface if needed.
        // This prevents the roomba from flipping forwards while going down slopes and transitioning to a different surface.
        float angle = (moveVector == Vector3.zero || moveVector2 == Vector3.zero) ? 0f : Mathf.Acos(Vector3.Dot(moveVector2, moveVector));
        angle *= Mathf.Sign(GetWalk(true));
        Debug.DrawLine(transform.position, transform.position + transform.up * angle, Color.red);
        Rigidbody.AddTorque(transform.right * -angle * Rigidbody.mass);

        Debug.DrawLine(transform.position, transform.position + MoveVector * 2f, Color.blue);

        // Move the roomba forwards or backwards along the floor tangent depending on the input.
        if (isColliding)
        {
            Rigidbody.velocity = (MoveVector * GetWalk() * MoveSpeed * (!canMoveAhead && Mathf.Sign(GetWalk()) == 1f ? 0f : 1f));
        }

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
        switch (selectedClass)
        {
            case RoombaClass.Cannon:
                foreach (Weapon weapon in GetComponentsInChildren<Weapon>())
                {
                    Debug.Log("Enabling Cannon");
                    if (!weapon.GetComponent<Cannon>())
                    {
                        weapon.gameObject.SetActive(false);
                    }
                }
                break;
            case RoombaClass.Stabbo:
                foreach (Weapon weapon in GetComponentsInChildren<Weapon>())
                {
                    Debug.Log("Enabling stabbo");
                    if (!weapon.GetComponent<Knife>())
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

        // Initialise lights.
        if (ExplosionLights != null)
        {
            ExplosionLightIntensities = new float[ExplosionLights.Length];
            for (int i = 0; i < ExplosionLights.Length; i++)
            {
                Light light = ExplosionLights[i];
                ExplosionLightIntensities[i] = light.intensity;
                light.range *= ExplosionLightRadius;
                light.enabled = false;
            }
        }

        // Find the camera object if not assigned.
        if (!Cam)
        {
            Cam = GetComponentInChildren<Camera>();
        }

        // Store the camera's initial transform.
        if (Cam)
        {
            InitialCameraOffset = Cam.transform.localPosition;
            initialCameraOrientation = Cam.transform.localRotation;
        }

        // Find the rigidbody component if not assigned.
        if (!Rigidbody)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        // Find the audio controller if not assigned.
        if(!RoombaAudioController)
        {
            RoombaAudioController = GetComponent<RoombaAudioController>();
        }

        if (!PlayerControlled)
        {
            // If this isn't our roomba, disable the camera audio listener so Unity doesn't complain.
            Cam.GetComponent<AudioListener>().enabled = false;

            // Prevent switching to the newly spawned roomba's camera by disabling it.
            Cam.enabled = false;
        }
        else
        {
            // Set the local player object instance in GameManager so it can be referenced by other scripts.
            if (GameManager.Singleton)
            {
                GameManager.Singleton.SpawnedPlayer = this;
            }
        }

        // For testing/offline play purposes, call any startup methods that would normally be invoked on OnNetworkSpawn.
        if(NetworkManager.Singleton?.IsConnectedClient != true && NetworkManager.Singleton?.IsHost != true)
        {
            SetWeapons();
        }
    }

    /// <summary>
    /// Occurs before <see cref="Start"/>
    /// </summary>
    public override void OnNetworkSpawn()
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
        clientRpcParams.Send.TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds;

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
        foreach (PlayerStats stats in FindObjectsOfType<PlayerStats>())
        {
            if (stats.OwnerClientId == victimClientId)
            {
                victimStats = stats;
            }
            if (stats.OwnerClientId == serverRpcParams.Receive.SenderClientId)
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
        if (victimStats && attackerStats)
        {
            Debug.Log($"Setting {GameManager.FromId(victimClientId).PlayerName.Value}'s LastAttackerId to {serverRpcParams.Receive.SenderClientId}");
            victimStats.LastAttacker.Value = $"{serverRpcParams.Receive.SenderClientId}";
        }
    }

    /// <summary>
    /// Every physics frame.
    /// </summary>
    void FixedUpdate()
    {
        // Use the dot product of the previous physics frame's forward-vector and the current right-vector.
        // This will let us check how much our velocity is aligned with either vector.
        slidingDot = 1f - Mathf.Abs(Vector3.Dot(fixedPrevForward, transform.right));
        isSlidingInterpolant = Mathf.InverseLerp(0f, 0.45f, slidingDot);

        fixedPrevForward = transform.forward;

        explicitVelocity = transform.position - prevPosition;
        prevPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerControlled)
        {
            gameObject.layer = LayerMask.NameToLayer("LocalPlayer");
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                // Prevent changing the layer on the orientation arrow as that screws up rendering.
                if (child.name != "PlayerOrientationArrow")
                {
                    child.gameObject.layer = LayerMask.NameToLayer("LocalPlayer");
                }
            }

            // Allow for preventing input using LockInput.
            // Otherwise run regular input-movement updates.
            if (!LockInput)
            {
                CameraLook();
                Movement();
            }

            if (IsOwner)
            {
                SyncVariablesServerRpc(Cam.transform.rotation);
            }

            // Suicide key.
            if (Input.GetKeyDown(KeyCode.F4))
            {
                GetComponent<PlayerStats>().BeginDieServerRpc();
            }
        }
        else
        {
            // If this is not a local player, update the movement from the server.
            ReplicateServerMovement();
        }

        RunFxAndAnimations();
    }

    /// <summary>
    /// Client-to-Server transform sync RPC as Netcode is fully server-authoritative.
    /// <para>TODO: There may be better ways of doing it.</para>
    /// </summary>
    /// <param name="camRotation"></param>
    /// <param name="rpcParams"></param>
    [ServerRpc]
    public void SyncVariablesServerRpc(Quaternion camRotation, ServerRpcParams rpcParams = default)
    {
        // Update the camera orientation for other clients.
        CameraRotation.Value = camRotation;
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

    Vector3 CalculateFloorMoveVector(RaycastHit hit)
    {
        return -CalculateSurfaceTangent(hit.normal);
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

    Vector3 CalculateFloorStrafeVector(RaycastHit hit)
    {
        return -Vector3.Cross(CalculateFloorMoveVector(hit), transform.up);
    }

    /// <summary>
    /// Checks for obstacles/inclines ahead using a Raycast.
    /// </summary>
    private void CheckAhead()
    {
        float rayLength = 1.1f;

        if (Physics.Raycast(new Ray(transform.position, transform.forward), out RaycastHit wallHit, 0.5f, ~(1 << LayerMask.NameToLayer("LocalPlayer"))))
        {
            if (LayerMask.LayerToName(wallHit.collider.gameObject.layer) != "Ignore Raycast")
            {
                // THIS CANMOVEAHEAD CHECK IS IMPORTANT. DON'T REMOVE IT UNLESS YOU WANT TO GO DEAF.
                // DON'T SAY I DIDN'T WARN YOU.
                if (canMoveAhead && RoombaAudioController)
                {
                    RoombaAudioController.BumpNoiseServerRpc(wallHit.normal, wallHit.point);
                }

                canMoveAhead = false;
            }
        }
        else
        {
            canMoveAhead = true;
        }

        Vector3 direction = transform.forward * Mathf.Sign(GetWalk(true));
        if(direction.sqrMagnitude < Rigidbody.velocity.sqrMagnitude)
        {
            direction = Rigidbody.velocity.normalized;
        }

        Debug.DrawLine(transform.position, transform.position + direction * rayLength);
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, rayLength, (1 << LayerMask.NameToLayer("Floor")));
        if (hits?.Length > 0)
        {
            Debug.DrawLine(hits[0].point, hits[0].point + hits[0].normal, Color.cyan);
            Vector3 hitTangent = CalculateFloorMoveVector(hits[0]);
            //Debug.Log($"Raycast dot: {Mathf.Abs(Vector3.Dot(hitTangent.normalized, transform.up))}");
            if (Mathf.Abs(Vector3.Dot(hitTangent.normalized, transform.up)) < 0.7f)
            {
                moveVector2 = hitTangent;
                strafeVector2 = CalculateFloorStrafeVector(hits[0]);

                canMoveAhead = true;
            }
        }
        else
        {
            moveVector2 = Vector3.zero;
            strafeVector2 = Vector3.zero;
        }
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
        Vector3 newMoveVector = CalculateFloorMoveVector(collision);
        if (Mathf.Abs(Vector3.Dot(newMoveVector, transform.up)) < 0.3f)
        {
            moveVector = newMoveVector;
            strafeVector = CalculateFloorStrafeVector(collision);
        }

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

    private void RunFxAndAnimations()
    {
        if (IsExploding)
        {
            // Update the explosion effect timer.
            ExplosionFxTimer -= Time.deltaTime;

            float inv = Mathf.InverseLerp(ExplosionFxTime, 0f, ExplosionFxTimer);

            // Activate the light (or step through multiple lights) using the interpolant value.
            int numLights = ExplosionLights.Length;
            float lightPeakUnit = 1f / (numLights + 1);
            for (int i = 0; i < numLights; i++)
            {
                float lightPeak = lightPeakUnit * (i + 1);
                float lightStart = i == 0 ? 0f : lightPeak - (lightPeakUnit * 1.5f);
                float progress = Mathf.InverseLerp(lightStart, lightPeak, inv);
                ExplosionLights[i].enabled = true;
                ExplosionLights[i].intensity = ExplosionLightIntensities[i] * progress;
            }

            if (ExplosionFxTimer <= 0f)
            {
                IsExploding = false;
            }
        }
    }
}
