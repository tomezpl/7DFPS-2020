using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoombaAudioController : NetworkBehaviour
{
    public AudioSource RoombaAccelerateAudioSrc;
    bool wasAcceleratingLastFrame = false;
    public RoombaControl Roomba;

    // Start is called before the first frame update
    void Start()
    {
        Roomba ??= GetComponent<RoombaControl>();
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

            wasAcceleratingLastFrame = accelerating;
        }
    }

    [ServerRpc]
    public void PlayAccelerateAudioServerRpc(ServerRpcParams rpcParams = default)
    {
        PlayAccelerateAudioClientRpc();
    }

    [ClientRpc]
    public void PlayAccelerateAudioClientRpc(ClientRpcParams rpcParams = default)
    {
        if(!RoombaAccelerateAudioSrc.isPlaying)
        {
            RoombaAccelerateAudioSrc.Play();
        }
    }
}
