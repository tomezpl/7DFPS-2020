using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    public int health = 100;
    public PlayerScore score;
    public PlayerStats lastAttacker;

    // UI
    public Text healthText = null, kdpText = null, winnerText = null;

    // Start is called before the first frame update
    void Start()
    {
        /*if (!PhotonView.Get(this) || !PhotonView.Get(this).IsMine)
        {
            return;
        }*/
        if (score == null)
        {
            score = new PlayerScore();
        }

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

            if(score != null)
            {
                kdpText.text = $"{score.Kills} Kills, {score.Deaths} Deaths, {score.TotalPoints} Points";
            }

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

    //[PunRPC]
    public void GiveScoreKills(int killsToGive = 1, bool sync = true)
    {
        if(score == null)
        {
            score = new PlayerScore();
        }

        score.Kills += killsToGive;

        if (sync)
        {
            SyncScoreWithLobby();
        }
    }

    //[PunRPC]
    public void GiveScoreDeaths(int deathsToGive = 1, bool sync = true)
    {
        if (score == null)
        {
            score = new PlayerScore();
        }

        score.Deaths += deathsToGive;

        if (sync)
        {
            SyncScoreWithLobby();
        }
    }

    void SyncScoreWithLobby()
    {
        LobbyManager lobbyManager = GameObject.Find("NetworkManager").GetComponent<LobbyManager>();
        string playerName = "fuckwad"/*PhotonView.Get(this).Owner.NickName*/;
        if (lobbyManager.PlayerScores.ContainsKey(playerName))
        {
            lobbyManager.PlayerScores[playerName] = score;
        }
        else
        {
            lobbyManager.PlayerScores.Add(playerName, score);
        }
    }

    public void Die()
    {
        health = -1;
        if (lastAttacker != null)
        {
            //PhotonView.Get(lastAttacker).RPC("GiveScoreKills", RpcTarget.All, new object[] { 1, true });
        }
        //PhotonView.Get(this).RPC("GiveScoreDeaths", RpcTarget.All, new object[] { 1, true });
        Camera.SetupCurrent(GameObject.Find("LobbyCamera").GetComponent<Camera>());
        GameObject.Find("NetworkManager").GetComponent<LobbyManager>().needToSpawn = true;
        //PhotonNetwork.Destroy(PhotonView.Get(this));
    }

    //[PunRPC]
    public void SetPlayerNameOverheadDisplay(string name)
    {
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
