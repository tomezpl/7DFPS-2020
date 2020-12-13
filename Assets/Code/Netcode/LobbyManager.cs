using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    public bool needToSpawn = true;

    // Is this a spawn (start of match) or respawn (after death)?
    bool _respawn = false;

    /// <summary>
    /// Called when the client connects to the master server. Joins a test room.
    /// TODO: Add custom rooms.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to master server");
        Photon.Realtime.RoomOptions options = new Photon.Realtime.RoomOptions();
        options.MaxPlayers = 8;
        options.IsVisible = true;
        PhotonNetwork.JoinOrCreateRoom("test", new Photon.Realtime.RoomOptions(), Photon.Realtime.TypedLobby.Default);
    }

    /// <summary>
    /// Called when a room is joined. Spawns the player.
    /// </summary>
    public override void OnJoinedRoom()
    {
        SpawnPlayer(Vector3.zero, Quaternion.identity);
        _respawn = true;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView view = PhotonView.Get(obj);
            if(view && view.IsMine)
            {
                view.RPC("SetWeapons", newPlayer, view.GetComponent<RoombaControl>().selectedClass);
            }
        }
    }

    public void SpawnPlayer(Vector3 position, Quaternion orientation)
    {
        GameObject obj = PhotonNetwork.Instantiate(playerPrefab.name, position, orientation);

        PhotonView.Get(obj).RPC("SetWeapons", RpcTarget.All, PhotonNetwork.CountOfPlayers - 1);

        needToSpawn = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    // Update is called once per frame
    void Update()
    {
        if(_respawn && needToSpawn)
        {
            SpawnPlayer(Vector3.zero, Quaternion.identity);
        }
    }
}
