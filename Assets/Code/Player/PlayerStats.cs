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

    public string serializedScore;

    // Start is called before the first frame update
    void Start()
    {
        foreach (Text text in GameObject.Find("HUD").GetComponentsInChildren<Text>())
        {
            switch(text.name)
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

            if(healthText && kdpText && winnerText)
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
            PlayerName.Value = NetworkManager.Singleton.GetComponent<LobbyManager>().PlayerName;
        }
        SetPlayerNameOverheadDisplay(PlayerName.Value);

        Score.OnValueChanged += (_, newScore) =>
        {
            Debug.Log($"Score changed. New score JSON:\n{newScore}");
            SyncScoreWithLobby();
        };
        SyncScoreWithLobby();
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<RoombaControl>().PlayerControlled)
        {
            if (health <= 0)
            {
                Die();
                healthText.text = "";
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

            PlayerScore score = _score;
            kdpText.text = $"{score.Kills} Kills, {score.Deaths} Deaths, {score.TotalPoints} Points";

            Dictionary<string, PlayerScore> players = GameObject.Find("NetworkManager").GetComponent<LobbyManager>().PlayerScores;
            string winner = players.Keys.FirstOrDefault(name => !players.Any(p => p.Key != name && p.Value.TotalPoints > players[name].TotalPoints));
            if(players.Count == 1)
            {
                winner = players.Keys.FirstOrDefault();
            }
            if(winner != default)
            {
                winnerText.text = $"1st place: {winner} ({players[winner].TotalPoints} points)";
            }
            else
            {
                winnerText.text = "";
            }
        }
        else
        {
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

    void SyncScoreWithLobby()
    {
        serializedScore = Score.Value;
        LobbyManager lobbyManager = GameObject.Find("NetworkManager").GetComponent<LobbyManager>();
        if (lobbyManager.PlayerScores.ContainsKey(PlayerName.Value))
        {
            lobbyManager.PlayerScores[PlayerName.Value] = _score;
        }
        else
        {
            lobbyManager.PlayerScores.Add(PlayerName.Value, _score);
        }
    }

    public void Die()
    {
        if(IsDying)
        {
            return;
        }

        IsDying = true;

        health = -1;
        if (LastAttackerId.Value != null)
        {
            GiveScoreKillsServerRpc((ulong)LastAttackerId.Value);
            //PhotonView.Get(lastAttacker).RPC("GiveScoreKills", RpcTarget.All, new object[] { 1, true });
        }
        //PhotonView.Get(this).RPC("GiveScoreDeaths", RpcTarget.All, new object[] { 1, true });
        GiveScoreDeathsServerRpc(OwnerClientId);
        Camera.SetupCurrent(GameObject.Find("LobbyCamera").GetComponent<Camera>());
        GameObject.Find("NetworkManager").GetComponent<LobbyManager>().needToSpawn = true;
        RequestDestroyPlayerServerRpc(OwnerClientId);
        //PhotonNetwork.Destroy(PhotonView.Get(this));
    }

    [ServerRpc]
    public void RequestDestroyPlayerServerRpc(ulong destroyedClientId, ServerRpcParams serverRpcParams = default)
    {
        DestroyPlayerClientRpc(destroyedClientId, new ClientRpcParams { Receive = default, Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClients.Keys.ToArray() } });
    }

    [ClientRpc]
    public void DestroyPlayerClientRpc(ulong destroyedClientId, ClientRpcParams clientRpcParams = default)
    {
        foreach(PlayerStats stats in FindObjectsOfType<PlayerStats>())
        {
            if(stats.OwnerClientId == destroyedClientId)
            {
                Destroy(stats.gameObject);
            }
        }
    }

    //[PunRPC]
    public void SetPlayerNameOverheadDisplay(string name)
    {
        if(IsOwner)
        {
            return;
        }

        Debug.Log($"Updating player {OwnerClientId}'s overhead display with name '{name}'");
        GetComponentsInChildren<TextMeshPro>().First(tmp => tmp.name == "PlayerName").text = name;
    }

    //[PunRPC]
    public void DetonateLithiumBomb()
    {
        Phone phone = GetComponentInChildren<Phone>();
        GetComponent<RoombaControl>().lockInput = true;
        phone._isExploding = true;
        phone._explosionFxTimer = phone.explosionFxTime;
    }

    private void OnEnable()
    {
        //PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        //PhotonNetwork.RemoveCallbackTarget(this);
    }

    /*public void OnEvent(EventData photonEvent)
    {
        //Debug.Log($"Received {photonEvent.Code}");

        // Check if the received event is about dealing damage to a player.
        if(photonEvent.Code == Events.DealDamageCode)
        {
            Debug.Log("This is DealDamage event");
            object[] data = (object[])photonEvent.CustomData;

            // Deserialize damage data.
            DamageData dmgData = new DamageData
            {
                AttackerViewId = (int)data[0],
                VictimViewId = (int)data[1],
                DamageDealt = (int)data[2]
            };
            //Debug.Log($"Victim was {dmgData.VictimViewId}. This is {PhotonView.Get(this).ViewID} ({this.name})");
            /*if (dmgData.VictimViewId == PhotonView.Get(this).ViewID)
            {
                health -= dmgData.DamageDealt;
                foreach(GameObject obj in FindObjectsOfType<GameObject>())
                {
                    if(PhotonView.Get(obj)?.ViewID == dmgData.AttackerViewId)
                    {
                        lastAttacker = obj.GetComponent<PlayerStats>();
                    }
                }
            }
        }
}*/
}
