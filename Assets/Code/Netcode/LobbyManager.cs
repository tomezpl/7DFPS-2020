using MLAPI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
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

    public Dictionary<string, PlayerScore> PlayerScores;

    public bool CanSpawn { get { return NetworkManager.Singleton.IsConnectedClient; } }

    /*public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (view && NetworkManager.Singleton.IsServer)
            {
                // Change all of this to use NetworkVariables
                view.RPC("SetWeapons", newPlayer, view.GetComponent<RoombaControl>().selectedClass);
                view.RPC("SetPlayerNameOverheadDisplay", newPlayer, _playerName);
                view.RPC("SetPlayerRoombaColour", newPlayer, _myColour.r, _myColour.g, _myColour.b);

                if (!PlayerScores.TryGetValue(view.Owner.NickName, out PlayerScore myScore))
                {
                    myScore = new PlayerScore();
                }
                view.RPC("GiveScoreKills", newPlayer, new object[] { myScore.Kills, true });
                view.RPC("GiveScoreDeaths", newPlayer, new object[] { myScore.Deaths, true });
            }
        }
    }*/

    public void SpawnPlayer(Vector3 position, Quaternion orientation, ulong clientId)
    {
        /*GameObject obj = PhotonNetwork.Instantiate(playerPrefab.name, position, orientation);
        localPlayerObj = obj;

        PhotonView.Get(obj).RPC("SetWeapons", RpcTarget.All, selectedClass);
        PhotonView.Get(obj).RPC("SetPlayerNameOverheadDisplay", RpcTarget.Others, _playerName);
        PhotonView.Get(obj).RPC("SetPlayerRoombaColour", RpcTarget.All, _myColour.r, _myColour.g, _myColour.b);
        PhotonView.Get(obj).RPC("GiveScoreKills", RpcTarget.All, new object[] { PlayerScores.TryGetValue(_playerName, out PlayerScore score) ? score.Kills : 0, false });
        PhotonView.Get(obj).RPC("GiveScoreDeaths", RpcTarget.All, new object[] { PlayerScores.TryGetValue(_playerName, out score) ? score.Deaths : 0, false });
        */
        Instantiate(playerPrefab, position, orientation).GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        needToSpawn = false;
        _showLobbyUi = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        //PhotonNetwork.PrefabPool = new RoombaRumblePrefabPool();

        //PhotonNetwork.ConnectUsingSettings();

        Camera.SetupCurrent(GameObject.Find("LobbyCamera").GetComponent<Camera>());
        _lobbyMenu = GameObject.Find("LobbyMenu");

        _classSelectMenu = GameObject.Find("ClassSelectMenu");
        _leftArrowClassBtn = _classSelectMenu.GetComponentsInChildren<Button>().First(btn => btn.name == "ClassLeftArrowBtn").gameObject;
        _rightArrowClassBtn = _classSelectMenu.GetComponentsInChildren<Button>().First(btn => btn.name == "ClassRightArrowBtn").gameObject;

        _respawnCvs = GameObject.Find("RespawnCanvas");

        _myColour = new Color(Random.value, Random.value, Random.value);

        PlayerScores = new Dictionary<string, PlayerScore>();

        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;

        //NetworkManager.Singleton.StartClient();

        UpdateClassImage(selectedClass);
    }

    private void ClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("heyooo from the client side");
            SpawnPlayer(Vector3.zero, Quaternion.identity, clientId);
        }
        else
        {
            // Set the respawn flag off as the server will be spawning us. This avoids UI being displayed after spawning.
            needToSpawn = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        _showLobbyUi = !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer;

        _lobbyMenu.SetActive(_showLobbyUi);

        _leftArrowClassBtn.SetActive(_showLeftArrowClassBtn && needToSpawn);
        _rightArrowClassBtn.SetActive(_showRightArrowClassBtn && needToSpawn);

        _respawnCvs.SetActive(_respawn && needToSpawn);

        if (_respawn && needToSpawn)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                SpawnPlayer(Vector3.zero, Quaternion.identity, NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    public void ClickedPlayHost()
    {
        Debug.Log("Starting game as host");
        ReadInputFields();

        {
            NetworkManager.Singleton.StartHost();
            SpawnPlayer(Vector3.zero, Quaternion.identity, NetworkManager.Singleton.LocalClientId);
        }

        //needToSpawn = true;
    }
    public void ClickedPlayClient()
    {
        Debug.Log("Connecting as client");
        ReadInputFields();

        {
            NetworkManager.Singleton.StartClient();
            //SpawnPlayer(Vector3.zero, Quaternion.identity, NetworkManager.Singleton.LocalClientId);
        }

        //needToSpawn = true;
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

        Debug.Log(classNum);

        tvRenderer.material.mainTexture = ClassTextures[(int)classNum];
    }
}
