using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoombaAudioController : NetworkBehaviour
{
    public RoombaControl Roomba;

    /// <summary>
    /// Sound effect when the Roomba launches from a stand-still.
    /// </summary>
    public AudioSource RoombaAccelerateAudioSrc;

    /// <summary>
    /// Minimal length factor of <see cref="RoombaAccelerateAudioSrc"/> before it can be played again.
    /// </summary>
    public float MinAcceleratePlayback = 1f;

    /// <summary>
    /// Base volume of <see cref="RoombaAccelerateAudioSrc"/> to use for attenuation.
    /// </summary>
    float accelerateAudioBaseVolume = 0f;

    /// <summary>
    /// Looped sound effect to use for the Roomba's driving hum.
    /// </summary>
    public AudioSource RoombaDriveLoopAudioSrc;
    
    /// <summary>
    /// Minimal multiplier of the base volume the roomba drive loop will play at.
    /// </summary>
    public float RoombaDriveLoopMinVolume = 0.2f;

    /// <summary>
    /// A delay (in seconds) to start the audio fade in for the roomba drive loop.
    /// </summary>
    public float DriveAudioStartDelay = 0.25f;

    /// <summary>
    /// A delay (in seconds) to finish the audio fade out for the roomba drive loop.
    /// </summary>
    public float DriveAudioEndDelay = 0.2f;

    /// <summary>
    /// Time (in seconds) it takes for the roomba drive loop to fade.
    /// </summary>
    public float FadeTime = 0.2f;

    /// <summary>
    /// Timers to control audio fading.
    /// </summary>
    float driveAudioStartTime = 0f, driveAudioEndTime = 0f;

    /// <summary>
    /// Base volume of the drive loop audio.
    /// </summary>
    float driveAudioBaseVolume = 0f;

    /// <summary>
    /// Is the Roomba currently driving?
    /// </summary>
    bool isDriving = false;

    /// <summary>
    /// Was acceleration input detected last frame?
    /// </summary>
    bool wasAcceleratingLastFrame = false;

    /// <summary>
    /// Roomba's position last physics frame for computing velocity.
    /// </summary>
    Vector3 lastPos = Vector3.zero;
    
    /// <summary>
    /// <see cref="velocity"/> synced over network.
    /// </summary>
    NetworkVariable<Vector3> SyncedVelocity = new NetworkVariable<Vector3>(NetworkVariableReadPermission.Everyone);

    /// <summary>
    /// Local velocity - if this Roomba is not owned by us, it will be read from <see cref="SyncedVelocity"/>.
    /// </summary>
    Vector3 velocity = Vector3.zero;

    /// <summary>
    /// Sound effect to use for Roomba explosion.
    /// </summary>
    public AudioSource RoombaExplosionAudioSrc;

    /// <summary>
    /// Was the Roomba in exploding state last frame? Used for detecting if <see cref="RoombaExplosionAudioSrc"/> should play.
    /// </summary>
    bool wasExplodingLastFrame = false;

    /// <summary>
    /// Sound effect to use for Roomba bumping into wall.
    /// </summary>
    public AudioSource RoombaBumpAudioSrc;

    /// <summary>
    /// Minimal absolute dot product between the wall hit normal and the roomba forward vec.
    /// </summary>
    public float MinBumpDot = 0.66f;

    // Start is called before the first frame update
    void Start()
    {
        Roomba ??= GetComponent<RoombaControl>();

        driveAudioBaseVolume = RoombaDriveLoopAudioSrc.volume;
        accelerateAudioBaseVolume = RoombaAccelerateAudioSrc.volume;

        lastPos = transform.position;

        SyncedVelocity.OnValueChanged = new NetworkVariable<Vector3>.OnValueChangedDelegate((oldVal, newVal) =>
        {
            velocity = newVal;
        });
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            velocity = transform.position - lastPos;

            lastPos = transform.position;

            UpdateVelocityServerRpc(velocity);
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            bool accelerating = Roomba.GetWalk(true) != 0f;

            if (!wasAcceleratingLastFrame && accelerating)
            {
                PlayAccelerateAudioServerRpc();
            }
            else if(wasAcceleratingLastFrame && !accelerating)
            {
                StopDrivingAudioServerRpc();
            }

            wasAcceleratingLastFrame = accelerating;
        }

        if(!wasExplodingLastFrame && Roomba.IsExploding)
        {
            RoombaExplosionAudioSrc.Play();
        }

        // If we're continously driving, keep delaying the fade out.
        if (isDriving)
        {
            driveAudioEndTime = Time.time + FadeTime + DriveAudioEndDelay;
        }

        ApplyEffects();

        wasExplodingLastFrame = Roomba.IsExploding;
    }

    /// <summary>
    /// Modulates/transitions the sound effects based on realtime parameters such as input, velocity etc.
    /// </summary>
    void ApplyEffects()
    {
        // Fade out the drive loop audio source if needed
        if (Time.time <= driveAudioStartTime + FadeTime)
        {
            RoombaDriveLoopAudioSrc.volume = Mathf.Lerp(RoombaDriveLoopMinVolume * driveAudioBaseVolume, driveAudioBaseVolume, Mathf.InverseLerp(driveAudioStartTime, driveAudioStartTime + FadeTime, Time.time));
        }
        else
        {
            RoombaDriveLoopAudioSrc.volume = Mathf.Lerp(driveAudioBaseVolume, driveAudioBaseVolume * RoombaDriveLoopMinVolume, Mathf.InverseLerp(driveAudioEndTime - FadeTime, driveAudioEndTime, Time.time));
        }

        // Tone down the acceleration sfx based on current speed.
        float speedT = Mathf.InverseLerp(0f, 0.003f, velocity.sqrMagnitude);

        RoombaAccelerateAudioSrc.volume = Mathf.Lerp(accelerateAudioBaseVolume, 0f, speedT);
        RoombaDriveLoopAudioSrc.pitch = Mathf.Lerp(0.75f, 1f, speedT);
    }

    /// <summary>
    /// Updates the computed velocity for other clients.
    /// </summary>
    /// <param name="clientSideVelocity">World-space velocity vector computed from physics updates.</param>
    /// <param name="rpcParams"></param>
    [ServerRpc]
    public void UpdateVelocityServerRpc(Vector3 clientSideVelocity, ServerRpcParams rpcParams = default)
    {
        SyncedVelocity.Value = clientSideVelocity;
    }

    [ServerRpc]
    public void StopDrivingAudioServerRpc(ServerRpcParams rpcParams = default)
    {
        StopDrivingAudioClientRpc();
    }

    [ClientRpc]
    public void StopDrivingAudioClientRpc(ClientRpcParams rpcParams = default)
    {
        isDriving = false;
    }

    [ServerRpc]
    public void PlayAccelerateAudioServerRpc(ServerRpcParams rpcParams = default)
    {
        PlayAccelerateAudioClientRpc();
    }

    [ClientRpc]
    public void PlayAccelerateAudioClientRpc(ClientRpcParams rpcParams = default)
    {
        if(!RoombaAccelerateAudioSrc.isPlaying || RoombaAccelerateAudioSrc.time >= RoombaAccelerateAudioSrc.clip.length * MinAcceleratePlayback)
        {
            RoombaAccelerateAudioSrc.Play();
        }

        if(!RoombaDriveLoopAudioSrc.isPlaying)
        {
            RoombaDriveLoopAudioSrc.Play();
        }

        isDriving = true;

        // Make sure to reset audio times for fadein to work correctly.
        if(Time.time >= driveAudioEndTime)
        {
            driveAudioStartTime = Time.time + DriveAudioStartDelay;
        }
    }

    /// <summary>
    /// Plays a bump noise based on collision info.
    /// </summary>
    /// <param name="collisionNormal">Normal vector from the collision point in world space.</param>
    /// <param name="collisionPoint">The collision point in world space.</param>
    public void BumpNoise(Vector3 collisionNormal, Vector3 collisionPoint)
    {
        float absoluteCollisionDot = Mathf.Abs(Vector3.Dot(Roomba.transform.forward, collisionNormal));
        float collisionT = Mathf.InverseLerp(MinBumpDot, 1f, absoluteCollisionDot);

        AudioSource collisionAudioSrc = new GameObject("RoombaBumpAudioSource").AddComponent<AudioSource>();
        collisionAudioSrc.transform.position = collisionPoint;

        collisionAudioSrc.clip = RoombaBumpAudioSrc.clip;
        collisionAudioSrc.volume = RoombaBumpAudioSrc.volume * collisionT;
        collisionAudioSrc.pitch = 2f - collisionT;
        collisionAudioSrc.SetCustomCurve(AudioSourceCurveType.CustomRolloff, RoombaBumpAudioSrc.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        collisionAudioSrc.spatialBlend = 1f;
        collisionAudioSrc.Play();

        Destroy(collisionAudioSrc.gameObject, collisionAudioSrc.clip.length);
    }

    [ClientRpc]
    public void BumpNoiseClientRpc(Vector3 collisionNormal, Vector3 collisionPoint, ClientRpcParams rpcParams = default)
    {
        BumpNoise(collisionNormal, collisionPoint);
    }

    [ServerRpc]
    public void BumpNoiseServerRpc(Vector3 collisionNormal, Vector3 collisionPoint, ServerRpcParams rpcParams = default)
    {
        BumpNoiseClientRpc(collisionNormal, collisionPoint, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.ConnectedClientsIds } });
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contactPoint = collision.GetContact(0);

        BumpNoise(contactPoint.normal, contactPoint.point);
    }
}
