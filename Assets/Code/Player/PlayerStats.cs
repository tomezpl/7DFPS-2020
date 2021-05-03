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

    /// <summary>
    /// The local player's <see cref="PlayerStats"/>. null if not found.
    /// </summary>
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

    /// <summary>
    /// Network-synchronised health value for this player.
    /// </summary>
    public NetworkVariableInt Health = new NetworkVariableInt(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    }, 100);

    public int health = 100;

    /// <summary>
    /// <para>Network-synchronised ID of the most recent attacker to this player.</para>
    /// <para>Don't use this outside of server RPCs; use the <see cref="LastAttackerId"/> cast instead.</para>
    /// </summary>
    public NetworkVariableString LastAttacker = new NetworkVariableString(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    }, "");

    /// <summary>
    /// A ulong parser for <see cref="LastAttacker"/> ID.
    /// </summary>
    public ulong? LastAttackerId
    {
        get => !string.IsNullOrWhiteSpace(LastAttacker.Value) ? (ulong?)ulong.Parse(LastAttacker.Value) : null;
    }

    /// <summary>
    /// A flag preventing the <see cref="Die"/> event being called multiple times on the server.
    /// </summary>
    public bool IsDying = false;

    
    /// <summary>
    /// UI text object.
    /// </summary>
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

        Health.OnValueChanged = (_, newHealth) =>
        {
            Debug.Log($"Updating Player{OwnerClientId}'s health to {newHealth}");
            health = newHealth;
        };

        SetPlayerNameOverheadDisplay(GameManager.FromId(OwnerClientId).PlayerName.Value);
        GameManager.FromId(OwnerClientId).PlayerName.OnValueChanged = (_, newName) => SetPlayerNameOverheadDisplay(newName);
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
        Debug.Log($"Last attacker was {LastAttackerId}");
        if (LastAttackerId != null && GameManager.FromId(LastAttackerId.Value) != null)
        {
            GameManager.Singleton.GiveScoreKillsServerRpc(LastAttackerId.Value);
        }

        // Call the server RPC to count a death for us.
        GameManager.Singleton.GiveScoreDeathsServerRpc(OwnerClientId);

        // Switch to lobby camera.
        Camera.SetupCurrent(GameObject.Find("LobbyCamera").GetComponent<Camera>());

        // Enter pending-spawn state.
        GameObject.Find("NetworkManager").GetComponent<LobbyManager>().DoesRequireSpawn = true;

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
}
