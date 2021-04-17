using MLAPI;
using MLAPI.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>A manager script to be instantiated for each player upon connecting to the server.</para>
/// <para>Essentially acts as an in-game identity of a player. Handles updating UI state, synchronising stats with LobbyManager, etc.</para>
/// <para>Resides as its own <see cref="NetworkObject"/> in the scene. Lifespan lasts through the player's connection to the game.</para>
/// </summary>
public class GameManager : NetworkBehaviour
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
    /// <para>A flag indicating whether the manager should send a respawn request RPC when triggered by the player.</para>
    /// <para>Needs to be set to false when the RPC is invoked.</para>
    /// <para>Prevents multiple respawn RPCs being sent.</para>
    /// </summary>
    public bool CanRequestSpawn = true;

    /// <summary>
    /// UI text objects.
    /// </summary>
    public Text healthText, kdpText, winnerText;

    // Start is called before the first frame update
    void Start()
    {
        // Find the UI text objects in the scene.
        foreach (Text text in GameObject.Find("HUD").GetComponentsInChildren<Text>())
        {
            switch (text.name)
            {
                case "Health":
                    healthText = text;
                    break;
                case "KDP":
                    kdpText = text;
                    break;
                case "Winner":
                    winnerText = text;
                    break;
            }

            if (healthText && kdpText && winnerText)
            {
                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner)
        {
            return;
        }

        // Check if the respawn flag is active.
        // This below statement will be true if the player has died at least once and is on the respawn screen.
        if (LobbyManager.Singleton.IsRespawn && LobbyManager.Singleton.DoesRequireSpawn)
        {
            if (Input.GetKeyDown(KeyCode.F) && CanRequestSpawn)
            {
                // Invoke a server RPC to request a spawn. Prevent multiple spawn RPCs being called.
                CanRequestSpawn = false;
                RequestPlayerSpawnServerRpc();
            }
        }

        UpdateStatsHud();
    }

    /// <summary>
    /// Updates the values of the stats text.
    /// </summary>
    void UpdateStatsHud()
    {
        // Update local player's stats text.
        if (PlayerStats.Local)
        {
            PlayerScore score = PlayerStats.Local._score;
            kdpText.text = $"{score.Kills} Kills, {score.Deaths} Deaths, {score.TotalPoints} Points";
        }

        // Find currently winning player to display their name and points.
        Dictionary<string, PlayerScore> players = LobbyManager.Singleton.PlayerScores;
        string winner = players.Keys.FirstOrDefault(name => !players.Any(p => p.Key != name && p.Value.TotalPoints > players[name].TotalPoints));
        if (players.Count == 1)
        {
            winner = players.Keys.FirstOrDefault();
        }
        if (winner != default)
        {
            winnerText.text = $"1st place: {winner} ({players[winner].TotalPoints} points)";
        }
        else
        {
            winnerText.text = "";
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
    public void RequestPlayerSpawnServerRpc(ServerRpcParams serverRpcParams = default)
    {
        LobbyManager.Singleton.SpawnPlayer(Vector3.zero, Quaternion.identity, serverRpcParams.Receive.SenderClientId);
    }
}
