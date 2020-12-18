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
    public Renderer tvRenderer;
    public GameObject localPlayerObj;

    // Images of different classes to display on the TV to select from.
    public Texture[] ClassTextures;

    // Is this a spawn (start of match) or respawn (after death)?
    bool _respawn = false;

    bool _showLobbyUi = false;

    string _playerName = "";
    string _roomName = "Test";

    GameObject _lobbyMenu;
    GameObject _classSelectMenu;
    GameObject _respawnCvs;

    public RoombaControl.RoombaClass selectedClass = RoombaControl.RoombaClass.Cannon;

    bool _showLeftArrowClassBtn { get { return (int)selectedClass > 0; } }
    bool _showRightArrowClassBtn { get { return (int)selectedClass < 2; } }

    GameObject _leftArrowClassBtn, _rightArrowClassBtn;

    Color _myColour;

    /// <summary>
    /// Called when the client connects to the master server. Joins a test room.
    /// TODO: Add custom rooms.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to master server");
        _showLobbyUi = true;

        UpdateClassImage(selectedClass);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        _showLobbyUi = true;
        _respawn = false;
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
                view.RPC("SetPlayerRoombaColour", newPlayer, _myColour.r, _myColour.g, _myColour.b);
                view.RPC("GiveScoreKills", newPlayer, view.GetComponent<PlayerStats>().score.Kills);
            }
        }
    }

    public void SpawnPlayer(Vector3 position, Quaternion orientation)
    {
        // This ensures the prefab is loaded in.
        Resources.Load(playerPrefab.name);

        GameObject obj = PhotonNetwork.Instantiate(playerPrefab.name, position, orientation);
        localPlayerObj = obj;

        PhotonView.Get(obj).RPC("SetWeapons", RpcTarget.All, selectedClass);
        PhotonView.Get(obj).RPC("SetPlayerNameOverheadDisplay", RpcTarget.Others, _playerName);
        PhotonView.Get(obj).RPC("SetPlayerRoombaColour", RpcTarget.All, _myColour.r, _myColour.g, _myColour.b);

        needToSpawn = false;
        _showLobbyUi = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();

        Camera.SetupCurrent(GameObject.Find("LobbyCamera").GetComponent<Camera>());
        _lobbyMenu = GameObject.Find("LobbyMenu");

        _classSelectMenu = GameObject.Find("ClassSelectMenu");
        _leftArrowClassBtn = _classSelectMenu.GetComponentsInChildren<Button>().First(btn => btn.name == "ClassLeftArrowBtn").gameObject;
        _rightArrowClassBtn = _classSelectMenu.GetComponentsInChildren<Button>().First(btn => btn.name == "ClassRightArrowBtn").gameObject;

        _respawnCvs = GameObject.Find("RespawnCanvas");

        _myColour = new Color(Random.value, Random.value, Random.value);
    }

    // Update is called once per frame
    void Update()
    {
        _lobbyMenu.SetActive(_showLobbyUi);

        _leftArrowClassBtn.SetActive(_showLeftArrowClassBtn && needToSpawn);
        _rightArrowClassBtn.SetActive(_showRightArrowClassBtn && needToSpawn);

        _respawnCvs.SetActive(_respawn && needToSpawn);

        if (_respawn && needToSpawn)
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

    public void GoToRightClass()
    {
        UpdateClassImage(++selectedClass);
    }
    public void GoToLeftClass()
    {
        UpdateClassImage(--selectedClass);
    }

    void UpdateClassImage(RoombaControl.RoombaClass classNum)
    {
        if(!tvRenderer)
        {
            return;
        }

        tvRenderer.material.mainTexture = ClassTextures[(int)classNum];
    }
}
