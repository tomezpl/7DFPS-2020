using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour, IOnEventCallback
{
    public int health = 100;
    public PlayerScore score;
    public PlayerStats lastAttacker;

    public Text healthText;

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonView.Get(this) || !PhotonView.Get(this).IsMine)
        {
            return;
        }
        score = new PlayerScore();

        foreach (Text text in GameObject.Find("HUD").GetComponentsInChildren<Text>())
        {
            if(text.name == "Health")
            {
                healthText = text;
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
            }

            healthText.enabled = true;
            healthText.text = $"Health: {health}";
        }
        else
        {
            if (healthText)
            {
                healthText.enabled = false;
            }
        }
    }

    [PunRPC]
    public void GiveScoreKills(int killsToGive = 1)
    {
        if(score == null)
        {
            score = new PlayerScore();
        }

        score.Kills += killsToGive;
    }

    public void Die()
    {
        if (lastAttacker != null)
        {
            PhotonView.Get(lastAttacker).RPC("GiveScoreKills", RpcTarget.All, 1);
        }
        Camera.SetupCurrent(GameObject.Find("LobbyCamera").GetComponent<Camera>());
        GameObject.Find("GameManager").GetComponent<LobbyManager>().needToSpawn = true;
        PhotonNetwork.Destroy(PhotonView.Get(this));
    }

    [PunRPC]
    public void SetPlayerNameOverheadDisplay(string name)
    {
        GetComponentsInChildren<TextMeshPro>().First(tmp => tmp.name == "PlayerName").text = name;
    }
    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnEvent(EventData photonEvent)
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
            Debug.Log($"Victim was {dmgData.VictimViewId}. This is {PhotonView.Get(this).ViewID} ({this.name})");
            if (dmgData.VictimViewId == PhotonView.Get(this).ViewID)
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
    }
}
