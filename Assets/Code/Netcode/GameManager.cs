using MLAPI;
using MLAPI.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    private static GameManager _singleton;
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

    public bool CanRequestSpawn = true;

    // HUD
    public Text healthText, kdpText, winnerText;

    // Start is called before the first frame update
    void Start()
    {
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

        if (LobbyManager.Singleton.respawn && LobbyManager.Singleton.needToSpawn)
        {
            if (Input.GetKeyDown(KeyCode.F) && CanRequestSpawn)
            {
                // Invoke a server RPC to request a spawn. Prevent multiple spawn RPCs being called.
                CanRequestSpawn = false;
                RequestPlayerSpawnServerRpc();
            }
        }

        DrawStatsHud();
    }

    void DrawStatsHud()
    {
        if (PlayerStats.Local)
        {
            PlayerScore score = PlayerStats.Local._score;
            kdpText.text = $"{score.Kills} Kills, {score.Deaths} Deaths, {score.TotalPoints} Points";
        }

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

    [ClientRpc]
    public void FinishDestroyPlayerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        LobbyManager.Singleton.respawn = true;
    }

    [ServerRpc]
    public void RequestPlayerSpawnServerRpc(ServerRpcParams serverRpcParams = default)
    {
        LobbyManager.Singleton.SpawnPlayer(Vector3.zero, Quaternion.identity, serverRpcParams.Receive.SenderClientId);
    }
}
