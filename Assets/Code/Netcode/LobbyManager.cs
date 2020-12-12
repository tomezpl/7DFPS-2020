using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to master server");
        Photon.Realtime.RoomOptions options = new Photon.Realtime.RoomOptions();
        options.MaxPlayers = 8;
        options.IsVisible = true;
        PhotonNetwork.JoinOrCreateRoom("test", new Photon.Realtime.RoomOptions(), Photon.Realtime.TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity);
    }

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
