using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>A manager script to be instantiated for each player upon connecting to the server.</para>
/// <para>Essentially acts as an in-game identity of a player. Handles updating UI state, synchronising stats with LobbyManager, etc.</para>
/// <para>Resides as its own <see cref="NetworkObject"/> in the scene. Lifespan lasts through the player's connection to the game.</para>
/// </summary>
public partial class GameManager : NetworkBehaviour
{
    private static GameManager _singleton;

    /// <summary>
    /// The local player's <see cref="GameManager"/> instance.
    /// </summary>
    public static GameManager Singleton
    {
        get
        {
            if(_singleton)
            {
                return _singleton;
            }

            foreach (GameManager manager in FindObjectsOfType<GameManager>())
            {
                if (manager.IsOwner)
                {
                    return _singleton = manager;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// This player's GUID in the match - those are generated per player session and can be used to differentiate splitscreen players.
    /// </summary>
    public Guid PlayerGuid { get; set; }

    /// <summary>
    /// This player's score, stored as a string for network transport.
    /// </summary>
    public NetworkVariable<FixedString32Bytes> SerializedScore = new NetworkVariable<FixedString32Bytes>(NetworkVariableReadPermission.Everyone, "");

    /// <summary>
    /// <para>Deserialized object based on <see cref="SerializedScore"/>.</para>
    /// <para>Avoid calling this getter multiple times to avoid costly parsing; use a variable.</para>
    /// </summary>
    public PlayerScore Score
    {
        get
        {
            return PlayerScore.FromString(SerializedScore.Value.ToString());
        }
    }

    /// <summary>
    /// <para>Network-synchronised name string for this player.</para>
    /// <para>This will be assigned from the player's lobby UI where they can set their name.</para>
    /// </summary>
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(NetworkVariableReadPermission.Everyone, string.Empty);

    /// <summary>
    /// <para>A flag indicating whether the manager should send a respawn request RPC when triggered by the player.</para>
    /// <para>Needs to be set to false when the RPC is invoked.</para>
    /// <para>Prevents multiple respawn RPCs being sent.</para>
    /// </summary>
    public bool CanRequestSpawn = true;

    /// <summary>
    /// Local player instance.
    /// </summary>
    public RoombaControl SpawnedPlayer = null;

    /// <summary>
    /// Client-side extension script for the gamemode logic.
    /// </summary>
    public GameModeGameManagerExtension GameModeExtensions;

    /// <summary>
    /// Searches for a specified client's <see cref="GameManager"/> instance.
    /// </summary>
    /// <param name="clientId">The client to search for.</param>
    /// <returns>Returns the <see cref="GameManager"/> object associated with <paramref name="clientId"/>, or null on failure.</returns>
    public static GameManager FromId(ulong clientId)
    {
        foreach(GameManager gameManager in FindObjectsOfType<GameManager>())
        {
            if(gameManager.OwnerClientId == clientId)
            {
                return gameManager;
            }
        }

        return null;
    }

    /// <summary>
    /// Searches for a specified player's <see cref="GameManager"/> instance.
    /// </summary>
    /// <param name="playerGuid">The player to search for.</param>
    /// <returns>Returns the <see cref="GameManager"/> object associated with <paramref name="playerGuid"/>, or null on failure.</returns>
    public static GameManager FromGuid(Guid playerGuid)
    {
        if (playerGuid != Guid.Empty)
        {
            foreach (GameManager gameManager in FindObjectsOfType<GameManager>())
            {
                if (gameManager.PlayerGuid == playerGuid)
                {
                    return gameManager;
                }
            }
        }

        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SyncPlayerNameServerRpc(LobbyManager.Singleton.PlayerName);
            RequestGameModeExtensionsServerRpc();
        }

        SerializedScore.OnValueChanged += (_, newScore) =>
        {
            Debug.Log($"Score changed. New score string:\n{newScore}");
            SyncScoreWithLobby();
        };
    }

    /// <summary>
    /// Updates the player name on the server.
    /// </summary>
    /// <param name="playerName"></param>
    /// <param name="rpcParams"></param>
    [ServerRpc]
    public void SyncPlayerNameServerRpc(string playerName, ServerRpcParams rpcParams = default)
    {
        PlayerName.Value = playerName;
    }

    /// <summary>
    /// Requests a client-side extension script for gamemode logic.
    /// </summary>
    /// <param name="rpcParams"></param>
    [ServerRpc]
    public void RequestGameModeExtensionsServerRpc(ServerRpcParams rpcParams = default)
    {
        AssignGameModeExtensionsClientRpc(LobbyManager.Singleton.CurrentGameMode.GetExtensionsType().FullName);
    }

    /// <summary>
    /// Creates the client-side gamemode logic extension script having received the script class type name from the server.
    /// </summary>
    /// <param name="extensionsTypeName"></param>
    /// <param name="rpcParams"></param>
    [ClientRpc]
    public void AssignGameModeExtensionsClientRpc(string extensionsTypeName, ClientRpcParams rpcParams = default)
    {
        GameModeExtensionsFactory.CreateExtensionsForPlayer(this, extensionsTypeName);
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner)
        {
            return;
        }
    }

    public void OnRespawnPressed()
    {
        // Check if the respawn flag is active.
        // This below statement will be true if the player has died at least once and is on the respawn screen.
        if (LobbyManager.Singleton.IsRespawn && LobbyManager.Singleton.DoesRequireSpawn)
        {
            if (CanRequestSpawn)
            {
                // Invoke a server RPC to request a spawn. Prevent multiple spawn RPCs being called.
                CanRequestSpawn = false;
                RequestPlayerSpawnServerRpc(PlayerGuid);
            }
        }
    }

    /// <summary>
    /// Notify the client that the death has been registered and the local player can now trigger a respawn.
    /// </summary>
    /// <param name="clientRpcParams"></param>
    [ClientRpc]
    public void FinishDestroyPlayerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        LobbyManager.Singleton.IsRespawn = true;
    }

    /// <summary>
    /// Request a spawn from the server.
    /// </summary>
    /// <param name="serverRpcParams"></param>
    [ServerRpc]
    public void RequestPlayerSpawnServerRpc(Guid playerGuid, ServerRpcParams serverRpcParams = default)
    {
        (Vector3 pos, Quaternion orientation) spawnPoint = FindSafestSpawnPoint();
        LobbyManager.Singleton.SpawnPlayer(spawnPoint.pos, spawnPoint.orientation, serverRpcParams.Receive.SenderClientId, FromGuid(playerGuid));
    }

    /// <summary>
    /// Searches for a spawn point as far away from players as possible.
    /// </summary>
    /// <returns>The spawn coords deemed to be safest based on distance to players.</returns>
    public (Vector3 pos, Quaternion orientation) FindSafestSpawnPoint()
    {
        try
        {
            RoombaControl[] players = FindObjectsOfType<RoombaControl>();
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

            float maxDistanceFound = 0f;
            int maxDistanceIndex = -1;

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                // Closest player's distance to this spawnpoint.
                // Initialise to -1 for the first player loop iteration.
                float closestPlayer = -1f;

                foreach (RoombaControl player in players)
                {
                    float distance = Vector3.Distance(spawnPoints[i].transform.position, player.transform.position);
                    if (closestPlayer < 0f)
                    {
                        closestPlayer = distance;
                    }
                    else
                    {
                        closestPlayer = Mathf.Min(closestPlayer, distance);
                    }

                    // Terminate early if any player is already closer than the global max distance found thus far.
                    if (closestPlayer < maxDistanceFound)
                    {
                        break;
                    }
                }

                if (closestPlayer > maxDistanceFound)
                {
                    maxDistanceFound = closestPlayer;
                    maxDistanceIndex = i;
                }
            }

            // If no safe spawn point was found, choose a random one.
            if (maxDistanceIndex < 0)
            {
                maxDistanceIndex = UnityEngine.Random.Range(0, spawnPoints.Length - 1);
            }

            Transform spawnPointTransform = spawnPoints[maxDistanceIndex].transform;
            return (spawnPointTransform.position, spawnPointTransform.rotation);
        }
        catch(Exception)
        {
            // Failsafe. This method doesn't need to run on each frame so using a try-catch should be fine,
            // and it saves us some headaches.
            return (Vector3.zero, Quaternion.identity);
        }
    }

    /// <summary>
    /// Updates <see cref="LobbyManager.PlayerScores"/> with this player's score.
    /// </summary>
    /// <param name="score">Optional. Use to override <see cref="SerializedScore"/>.</param>
    public void SyncScoreWithLobby(PlayerScore score = null)
    {
        if (score == null)
        {
            score = Score;
        }

        if (!string.IsNullOrWhiteSpace(PlayerName.Value.ToString()))
        {
            GameManager.Singleton.GameModeExtensions.PlayerScores[PlayerName.Value.ToString()] = score;
        }
    }
}
