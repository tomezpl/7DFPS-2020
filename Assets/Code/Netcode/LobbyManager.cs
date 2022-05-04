using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// A manager script to handle connecting to a game and starting it.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    private static LobbyManager _singleton;

    /// <summary>
    /// The <see cref="LobbyManager"/> instance in the current scene.
    /// </summary>
    public static LobbyManager Singleton
    {
        get
        {
            if (_singleton)
            {
                return _singleton;
            }

            try
            {
                return _singleton = GameObject.Find("NetworkManager").GetComponent<LobbyManager>();
            }
            catch(Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Prefab to spawn the player as.
    /// </summary>
    public GameObject PlayerPrefab;

    /// <summary>
    /// A GameManager instance to spawn for the connected player. Only spawned once.
    /// </summary>
    public GameObject GameManagerPrefab;

    /// <summary>
    /// <para>Indicates whether the player requires being spawned in the scene.</para>
    /// <para>This will be true on starting the game and after death (provided respawns are allowed).</para>
    /// </summary>
    public bool DoesRequireSpawn = true;

    /// <summary>
    /// <para>Class selection screen <see cref="Renderer"/>.</para>
    /// <para>Currently this will be the TV screen in the Apartment level.</para>
    /// </summary>
    public Renderer ClassSelectionRenderer;

    /// <summary>
    /// <para>A reference to the spawned local player object.</para>
    /// <para>Use this instead of iterating through GameObjects in the scene every time.</para>
    /// </summary>
    public GameObject LocalPlayerObject;

    /// <summary>
    /// Images of different classes to display on the TV to select from.
    /// </summary>
    public Texture[] ClassTextures;

    /// <summary>
    /// <para>Indicates if the player should spawn for the first time, or respawn.</para>
    /// <para>true if the player already died once and will only be respawned from now on.</para>
    /// </summary>
    public bool IsRespawn = false;

    /// <summary>
    /// Indicates whether the lobby UI should be displayed or not.
    /// </summary>
    public bool IsLobbyUiShown = false;

    private string _enteredPlayerName = "";

    /// <summary>
    /// Player name read from the input field in the lobby UI.
    /// </summary>
    public string PlayerName { get { return _enteredPlayerName; } }

    private string _enteredIpAddress = "0.0.0.0:7777";

    /// <summary>
    /// Server IP address read from the input field in the lobby UI.
    /// </summary>
    public string EnteredIpAddress { get { return _enteredIpAddress; } }

    /// <summary>
    /// Fallback address to connect to if no IP address was specified.
    /// </summary>
    public const string DefaultHostAddress = "127.0.0.1";

    private string _enteredHostPort = "7777";

    /// <summary>
    /// Port number to host on, entered by the player.
    /// </summary>
    public string EnteredHostPort { get => _enteredHostPort; }

    /// <summary>
    /// Fallback port to connect on if no IP address was specified.
    /// </summary>
    public const string DefaultHostPort = "7777";

    /// <summary>
    /// Lobby menu Canvas object.
    /// </summary>
    GameObject lobbyMenu;

    /// <summary>
    /// Class select menu Canvas object.
    /// </summary>
    GameObject classSelectMenu;

    /// <summary>
    /// Respawn screen Canvas object.
    /// </summary>
    GameObject respawnCvs;

    /// <summary>
    /// Player class selected in the lobby/spawn UI to spawn as.
    /// </summary>
    public RoombaControl.RoombaClass SelectedClass = RoombaControl.RoombaClass.Cannon;

    /// <summary>
    /// true if the previous class button should be shown on the class selection UI.
    /// </summary>
    bool showLeftArrowClassBtn { get { return (int)SelectedClass > 0; } }

    /// <summary>
    /// true if the next class button should be shown on the class selection UI.
    /// </summary>
    bool showRightArrowClassBtn { get { return (int)SelectedClass < 2; } }

    /// <summary>
    /// Class selection buttons.
    /// </summary>
    GameObject leftArrowClassBtn, rightArrowClassBtn;

    /// <summary>
    /// Colour to assign to the player.
    /// <para>TODO: This needs to be a <see cref="MLAPI.NetworkVariable.NetworkVariableColor"/>!</para>
    /// </summary>
    public Color MyColour;

    /// <summary>
    /// Can the local player spawn?
    /// </summary>
    public bool CanSpawn { get { return NetworkManager.Singleton.IsConnectedClient; } }

    /// <summary>
    /// Current gamemode instance.
    /// </summary>
    public MultiplayerGameMode CurrentGameMode = null;

    /// <summary>
    /// Server/Host only: Spawns a player prefab for a client and assigns it ownership.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="orientation"></param>
    /// <param name="clientId"></param>
    public void SpawnPlayer(Vector3 position, Quaternion orientation, ulong clientId)
    {
        GameObject instance = Instantiate(PlayerPrefab, position, orientation);

        instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Activate the lobby camera.
        //Camera.SetupCurrent(GameObject.Find("LobbyCamera").GetComponent<Camera>());

        // Initialise the lobby menu Canvas.
        lobbyMenu = GameObject.Find("LobbyMenu");

        // Initialise the class selection menu Canvas and buttons.
        classSelectMenu = GameObject.Find("ClassSelectMenu");
        leftArrowClassBtn = classSelectMenu.GetComponentsInChildren<Button>().First(btn => btn.name == "ClassLeftArrowBtn").gameObject;
        rightArrowClassBtn = classSelectMenu.GetComponentsInChildren<Button>().First(btn => btn.name == "ClassRightArrowBtn").gameObject;

        // Initialise the respawn screen Canvas.
        respawnCvs = GameObject.Find("RespawnCanvas");

        // Assign a random colour for the player.
        // TODO: Change this to NetworkVariableColour in order for it to sync.
        MyColour = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);


        // Add an event handler for clients connecting & disconnecting.
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

        // Display the preselected class image.
        UpdateClassImage(SelectedClass);

        if (DataStore.MultiplayerInfo.IsHost)
        {
            StartedHost();
        }
    }

    /// <summary>
    /// <para>Event handler for clients connecting to the server/host.</para>
    /// <para>Is only fired for actual clients; omitted for the host player startup event.</para>
    /// </summary>
    /// <param name="clientId">The ID of the connecting client.</param>
    private void ClientConnected(ulong clientId)
    {
        // Reset the respawn flag in case the player was previously in a game.
        IsRespawn = false;

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"Server: A client with id {clientId} just connected. Spawning a GameManager and player instance for them.");

            // Spawn a GameManager instance for the Client player.
            SpawnGameManager(clientId);

            // Spawn the initial PlayerPrefab instance for the Client player.
            (Vector3 pos, Quaternion orientation) spawnPoint = GameManager.Singleton.FindSafestSpawnPoint();
            SpawnPlayer(spawnPoint.pos, spawnPoint.orientation, clientId);
        }
        else
        {
            Debug.Log($"Client: Connected as a client succesfully with id {clientId}.");

            // Set the spawn flag off as the server will be spawning us immediately.
            // This avoids UI being displayed after spawning.
            DoesRequireSpawn = false;
        }
    }

    /// <summary>
    /// Event handler for clients disconnecting from the server.
    /// </summary>
    /// <param name="clientId">The ID of the disconnecting client.</param>
    private void ClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // Remove the player from the gamemode.
            CurrentGameMode?.RemovePlayer(clientId);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetSceneByName("MainMenu").buildIndex);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Only show the lobby UI if the player is not connected to a game yet.
        IsLobbyUiShown = !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer;
        lobbyMenu.SetActive(IsLobbyUiShown);

        // Display the correct class selection buttons.
        leftArrowClassBtn.SetActive(showLeftArrowClassBtn && DoesRequireSpawn);
        rightArrowClassBtn.SetActive(showRightArrowClassBtn && DoesRequireSpawn);

        // Display the respawn UI if the next spawn is meant to be a respawn, not an initial spawn.
        respawnCvs.SetActive(IsRespawn && DoesRequireSpawn);
    }

    /// <summary>
    /// Starts the game as a Host (Server + Client).
    /// </summary>
    public void StartedHost()
    {
        Debug.Log("Starting game as host");

        // Bind the user-provided port number to the network transport to host a game on a specified port.
        UNetTransport transport = NetworkManager.Singleton.GetComponent<UNetTransport>();
        transport.ConnectPort = DataStore.MultiplayerInfo.PortNumber;
        transport.ServerListenPort = DataStore.MultiplayerInfo.PortNumber;

        // Set the gamemode.
        SetGameMode<CleanupJobGameMode>();

        NetworkManager.Singleton.StartHost();
    }

    /// <summary>
    /// Spawn a <see cref="GameManager"/> object for a newly connected client. This is only spawned once per player connection.
    /// </summary>
    /// <param name="clientId"></param>
    private void SpawnGameManager(ulong clientId)
    {
        GameObject instantiatedGameManager = Instantiate(GameManagerPrefab);

        instantiatedGameManager.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

        // Add to the gamemode if needed.
        CurrentGameMode?.AddPlayer(instantiatedGameManager.GetComponent<GameManager>());
    }

    /// <summary>
    /// Set the current server-side gamemode.
    /// </summary>
    /// <typeparam name="GameModeType"></typeparam>
    /// <param name="gameModeInstance">A <see cref="MultiplayerGameMode"/> instance to set as the current gamemode.</param>
    private void SetGameMode<GameModeType>(GameModeType gameModeInstance) where GameModeType : MultiplayerGameMode
    {
        if (CurrentGameMode)
        {
            Destroy(CurrentGameMode);
        }

        CurrentGameMode = gameModeInstance;
    }

    /// <summary>
    /// Sets the current server-side gamemode.
    /// </summary>
    /// <typeparam name="GameModeType">A <see cref="MultiplayerGameMode"/> implementation to instantiate & set as current gamemode.</typeparam>
    private void SetGameMode<GameModeType>() where GameModeType : MultiplayerGameMode
    {
        SetGameMode(gameObject.AddComponent<GameModeType>());
    }

    /// <summary>
    /// Starts the game by connecting as a Client to the specified Server.
    /// </summary>
    public void StartedClient()
    {
        Debug.Log("Connecting as client");

        // Bind network transport settings.
        UNetTransport transport = NetworkManager.Singleton.GetComponent<UNetTransport>();
        
        // Bind port number.
        transport.ConnectPort = DataStore.MultiplayerInfo.PortNumber;
        transport.ServerListenPort = DataStore.MultiplayerInfo.PortNumber;

        // Bind IP address.
        string ipAddress = DataStore.MultiplayerInfo.IpAddress;
        
        if (ipAddress != null)
        {
            transport.ConnectAddress = ipAddress;
        }

        NetworkManager.Singleton.StartClient();
    }

    /// <summary>
    /// Reads data from text input fields in the lobby UI.
    /// </summary>
    public void ReadInputFields()
    {
        lobbyMenu.SetActive(true);
        IsLobbyUiShown = true;

        foreach (InputField field in lobbyMenu.GetComponentsInChildren<InputField>())
        {
            if (field.name == "IpAddress")
            {
                _enteredIpAddress = field.text;
            }
            else if (field.name == "PlayerName")
            {
                _enteredPlayerName = field.text;
            }
            else if(field.name == "PortNumber")
            {
                _enteredHostPort = field.text;
            }
        }
    }

    /// <summary>
    /// Event handler to navigate to the next class on the class selection menu.
    /// </summary>
    public void GoToRightClass() => UpdateClassImage(++SelectedClass);

    /// <summary>
    /// Event handler to navigate to the previous class on the class selection menu.
    /// </summary>
    public void GoToLeftClass() => UpdateClassImage(--SelectedClass);

    /// <summary>
    /// Update the currently displayed class image.
    /// </summary>
    /// <param name="classNum">
    /// <para>A RoombaClass to display.</para>
    /// <para>Indices in the enum need cannot be out of range of the ClassTextures array.</para>
    /// </param>
    void UpdateClassImage(RoombaControl.RoombaClass classNum)
    {
        if (!ClassSelectionRenderer)
        {
            return;
        }

        Debug.Log($"Selected class: {classNum} ({(int)classNum})");

        ClassSelectionRenderer.material.mainTexture = ClassTextures[(int)classNum];
    }
}
