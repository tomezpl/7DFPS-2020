using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : NetworkBehaviour
{
    private static PlayerStats _local;
    public static PlayerStats Local
    {
        get
        {
            if(_local)
            {
                return _local;
            }

            foreach(PlayerStats stats in FindObjectsOfType<PlayerStats>())
            {
                if(stats.IsOwner)
                {
                    return _local = stats;
                }
            }

            return null;
        }
    }

    public NetworkVariableInt Health = new NetworkVariableInt(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    }, 100);

    public NetworkVariableString PlayerName = new NetworkVariableString(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    }, "");

    public int health = 100;

    /// <summary>
    /// This player's score, stored as JSON for network transport.
    /// </summary>
    public NetworkVariableString Score = new NetworkVariableString(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    }
    , default);

    public PlayerScore _score
    {
        get
        {
            return PlayerScore.FromString(Score.Value);
        }
    }

    public NetworkVariable<ulong?> LastAttackerId = new NetworkVariable<ulong?>(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    }, null);

    public PlayerStats LastAttacker
    {
        get
        {
            foreach(PlayerStats stats in FindObjectsOfType<PlayerStats>())
            {
                if (stats.OwnerClientId == LastAttackerId.Value)
                {
                    return stats;
                }
            }

            return null;
        }
    }

    // A flag preventing the Die event being called multiple times on the server.
    public bool IsDying = false;

    // UI
    public Text healthText = null, kdpText = null, winnerText = null;

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

    public override void NetworkStart()
    {
        health = Health.Value;

        Health.OnValueChanged += (_, newHealth) =>
        {
            Debug.Log($"Updating Player{OwnerClientId}'s health to {newHealth}");
            health = newHealth;
        };

        PlayerName.OnValueChanged += (_, newName) => SetPlayerNameOverheadDisplay(newName);
        if (IsOwner)
        {
            PlayerName.Value = LobbyManager.Singleton.PlayerName;
        }
        SetPlayerNameOverheadDisplay(PlayerName.Value);

        if (IsOwner)
        {
            if (LobbyManager.Singleton.PlayerScores.TryGetValue(PlayerName.Value, out PlayerScore existingScore))
            {
                Debug.Log($"Found an existing score for Player {PlayerName.Value}. The score is {existingScore}.");
                RestoreScoreServerRpc(existingScore.ToString());
            }
        }

        Score.OnValueChanged += (_, newScore) =>
        {
            Debug.Log($"Score changed. New score JSON:\n{newScore}");
            SyncScoreWithLobby();
        };
        SyncScoreWithLobby();
    }

    // TODO: Rewrite to pack this into the Stream and set in NetworkStart
    [ServerRpc]
    void RestoreScoreServerRpc(string score, ServerRpcParams serverRpcParams = default)
    {
        Score.Value = score;
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<RoombaControl>().PlayerControlled)
        {
            if (health <= 0)
            {
                healthText.text = "";
                Die();
            }

            healthText.enabled = true;
            if (health <= 0)
            {
                healthText.text = "";
            }
            else
            {
                healthText.text = $"Health: {health}";
            }
        }
    }

    [ServerRpc]
    public void GiveScoreKillsServerRpc(ulong clientId, int killsToGive = 1, ServerRpcParams serverRpcParams = default)
    {
        foreach (PlayerStats stats in FindObjectsOfType<PlayerStats>())
        {
            if (stats.OwnerClientId == clientId)
            {
                Debug.Log($"Giving client {clientId} {killsToGive} kills");
                PlayerScore score = stats._score;
                score.Kills += killsToGive;
                stats.Score.Value = score.ToString();
                break;
            }
        }
    }

    [ServerRpc]
    public void GiveScoreDeathsServerRpc(ulong clientId, int deathsToGive = 1, ServerRpcParams serverRpcParams = default)
    {
        foreach (PlayerStats stats in FindObjectsOfType<PlayerStats>())
        {
            if (stats.OwnerClientId == clientId)
            {
                Debug.Log($"Giving client {clientId} {deathsToGive} deaths");
                PlayerScore score = stats._score;
                score.Deaths += deathsToGive;
                Debug.Log($"Sending new score: {score.ToString()}");
                stats.Score.Value = score.ToString();
                break;
            }
        }
    }

    void SyncScoreWithLobby(PlayerScore score = null)
    {
        if(score == null)
        {
            score = _score;
        }

        LobbyManager lobbyManager = GameObject.Find("NetworkManager").GetComponent<LobbyManager>();
        if (lobbyManager.PlayerScores.ContainsKey(PlayerName.Value))
        {
            lobbyManager.PlayerScores[PlayerName.Value] = score;
        }
        else
        {
            lobbyManager.PlayerScores.Add(PlayerName.Value, score);
        }
    }

    /// <summary>
    /// <para>Kills the player and awards the kill to the last attacker.</para>
    /// <para>Also increments this player's death counter and switches to class selection/respawn state.</para>
    /// </summary>
    public void Die()
    {
        // Should only be called once per death.
        if(IsDying)
        {
            return;
        }

        IsDying = true;

        // Make sure the health is kept below 0. Technically unnecessary, but I'm paranoid...
        health = -1;

        // If another player killed us, call the server RPC to give them a kill.
        if (LastAttackerId.Value != null)
        {
            GiveScoreKillsServerRpc((ulong)LastAttackerId.Value);
        }

        // Call the server RPC to count a death for us.
        GiveScoreDeathsServerRpc(OwnerClientId);

        // Switch to lobby camera.
        Camera.SetupCurrent(GameObject.Find("LobbyCamera").GetComponent<Camera>());

        // Enter pending-spawn state.
        GameObject.Find("NetworkManager").GetComponent<LobbyManager>().DoesRequireSpawn = true;

        // Hotfix for the old score being synced before the GiveScore RPCs finish:
        PlayerScore tempScore = PlayerScore.FromString(Score.Value);
        tempScore.Deaths++;

        // Sync using a local copy of the score so we don't have to wait for the RPCs to finish.
        // TODO: this could use putting in GameManager instead to avoid this abundant use of RPCs.
        SyncScoreWithLobby(tempScore);

        // Call a server RPC to destroy the player.
        RequestDestroyPlayerServerRpc();
    }

    /// <summary>
    /// Despawns this object on the server and notifies this player's LobbyManager to enter respawn state.
    /// </summary>
    /// <param name="serverRpcParams"></param>
    [ServerRpc]
    public void RequestDestroyPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        NetworkObject.Despawn(true);

        // Notify the player that they should switch to class selection/respawn state.
        GameManager.Singleton.FinishDestroyPlayerClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId } } });
    }

    /// <summary>
    /// Updates the player's rendered name text to <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name to display above the player.</param>
    public void SetPlayerNameOverheadDisplay(string name)
    {
        if(IsOwner)
        {
            return;
        }

        Debug.Log($"Updating player {OwnerClientId}'s overhead display with name '{name}'");
        GetComponentsInChildren<TextMeshPro>().First(tmp => tmp.name == "PlayerName").text = name;
    }

    // TODO: Move this to Phone.cs?
    public void DetonateLithiumBomb()
    {
        Phone phone = GetComponentInChildren<Phone>();
        GetComponent<RoombaControl>().LockInput = true;
        phone.isExploding = true;
        phone.explosionFxTimer = phone.ExplosionFxTime;
    }
}
