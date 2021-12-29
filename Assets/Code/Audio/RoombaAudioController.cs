using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoombaAudioController : NetworkBehaviour
{
    public AudioSource RoombaAccelerateAudioSrc;

    public float MinAcceleratePlayback = 1f;
    float accelerateAudioBaseVolume = 0f;

    public AudioSource RoombaDriveLoopAudioSrc;

    public float DriveAudioStartDelay = 0.25f;
    public float DriveAudioEndDelay = 0.2f;

    public float FadeTime = 0.2f;
    float driveAudioStartTime = 0f, driveAudioEndTime = 0f;
    float driveAudioBaseVolume = 0f;
    bool isDriving = false;

    bool wasAcceleratingLastFrame = false;
    public RoombaControl Roomba;

    Vector3 lastPos = Vector3.zero;
    Vector3 velocity = Vector3.zero;

    public AudioSource RoombaExplosionAudioSrc;
    bool wasExplodingLastFrame = false;

    // Start is called before the first frame update
    void Start()
    {
        Roomba ??= GetComponent<RoombaControl>();

        driveAudioBaseVolume = RoombaDriveLoopAudioSrc.volume;
        accelerateAudioBaseVolume = RoombaAccelerateAudioSrc.volume;

        lastPos = transform.position;
    }

    private void FixedUpdate()
    {
        velocity = transform.position - lastPos;

        lastPos = transform.position;
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

        Debug.Log(velocity.sqrMagnitude);

        // If we're continously driving, keep delaying the fade out.
        if (isDriving)
        {
            driveAudioEndTime = Time.time + FadeTime + DriveAudioEndDelay;
        }

        ApplyEffects();

        wasExplodingLastFrame = Roomba.IsExploding;
    }

    void ApplyEffects()
    {
        // Fade out the drive loop audio source if needed
        if (Time.time <= driveAudioStartTime + FadeTime)
        {
            RoombaDriveLoopAudioSrc.volume = Mathf.Lerp(0f, driveAudioBaseVolume, Mathf.InverseLerp(driveAudioStartTime, driveAudioStartTime + FadeTime, Time.time));
        }
        else
        {
            RoombaDriveLoopAudioSrc.volume = Mathf.Lerp(driveAudioBaseVolume, 0f, Mathf.InverseLerp(driveAudioEndTime - FadeTime, driveAudioEndTime, Time.time));
        }

        // Tone down the acceleration sfx based on current speed.
        RoombaAccelerateAudioSrc.volume = Mathf.Lerp(accelerateAudioBaseVolume, 0f, Mathf.InverseLerp(0f, 0.005f, velocity.sqrMagnitude));
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
}
