using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    public bool needToSpawn = true;

    // Is this a spawn (start of match) or respawn (after death)?
    bool _respawn = false;

    bool _showLobbyUi = false;

    string _playerName = "";
    string _roomName = "Test";

    GameObject _lobbyMenu;

    /// <summary>
    /// Called when the client connects to the master server. Joins a test room.
    /// TODO: Add custom rooms.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to master server");
        _showLobbyUi = true;
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
                view.RPC("SetPlayerNameOverheadDisplay", newPlayer, _playerName);
            }
        }
    }

    public void SpawnPlayer(Vector3 position, Quaternion orientation)
    {
        GameObject obj = PhotonNetwork.Instantiate(playerPrefab.name, position, orientation);

        PhotonView.Get(obj).RPC("SetWeapons", RpcTarget.All, PhotonNetwork.CountOfPlayers - 1);
        PhotonView.Get(obj).RPC("SetPlayerNameOverheadDisplay", RpcTarget.Others, _playerName);

        needToSpawn = false;
        _showLobbyUi = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();

        Camera.SetupCurrent(GameObject.Find("LobbyCamera").GetComponent<Camera>());
        _lobbyMenu = GameObject.Find("LobbyMenu");
    }

    // Update is called once per frame
    void Update()
    {
        _lobbyMenu.SetActive(_showLobbyUi);

        if(_respawn && needToSpawn)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                SpawnPlayer(Vector3.zero, Quaternion.identity);
            }
        }
    }

    public void ClickedPlay()
    {
        Photon.Realtime.RoomOptions options = new Photon.Realtime.RoomOptions();
        options.MaxPlayers = 8;
        options.IsVisible = true;
        ReadInputFields();
        PhotonNetwork.NickName = _playerName;
        PhotonNetwork.JoinOrCreateRoom(_roomName, new Photon.Realtime.RoomOptions(), Photon.Realtime.TypedLobby.Default);
    }

    public void ReadInputFields()
    {
        _lobbyMenu.SetActive(true);
        _showLobbyUi = true;

        foreach(InputField field in _lobbyMenu.GetComponentsInChildren<InputField>())
        {
            if(field.name == "RoomName")
            {
                _roomName = field.text;
            }
            else if(field.name == "PlayerName")
            {
                _playerName = field.text;
            }
        }
    }
}
