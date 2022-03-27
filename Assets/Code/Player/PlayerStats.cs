using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;

public class PlayerStats : NetworkBehaviour
{
    private static PlayerStats _local;

    public GameObject PlayerRemainsPrefab;

    bool remainsSpawned = false;

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
    public NetworkVariable<int> Health = new NetworkVariable<int>(NetworkVariableReadPermission.Everyone, 100);

    public int health = 100;

    /// <summary>
    /// <para>Network-synchronised ID of the most recent attacker to this player.</para>
    /// <para>Don't use this outside of server RPCs; use the <see cref="LastAttackerId"/> cast instead.</para>
    /// </summary>
    public NetworkVariable<FixedString32Bytes> LastAttacker = new NetworkVariable<FixedString32Bytes>(NetworkVariableReadPermission.Everyone, "");

    /// <summary>
    /// A ulong parser for <see cref="LastAttacker"/> ID.
    /// </summary>
    public ulong? LastAttackerId
    {
        get => !string.IsNullOrWhiteSpace(LastAttacker.Value.ToString()) ? (ulong?)ulong.Parse(LastAttacker.Value.ToString()) : null;
    }

    /// <summary>
    /// A flag preventing the <see cref="BeginDieServerRpc"/> event being called multiple times on the server.
    /// </summary>
    public bool IsDying = false;

    
    /// <summary>
    /// UI text object.
    /// </summary>
    public Text healthText = null, kdpText = null, winnerText = null;

    bool wasExplodingLastFrame = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    public override void OnNetworkSpawn()
    {
        health = Health.Value;

        Health.OnValueChanged = (_, newHealth) =>
        {
            Debug.Log($"Updating Player{OwnerClientId}'s health to {newHealth}");
            health = newHealth;
        };

        SetPlayerNameOverheadDisplay(GameManager.FromId(OwnerClientId).PlayerName.Value.ToString());
        GameManager.FromId(OwnerClientId).PlayerName.OnValueChanged = (_, newName) => SetPlayerNameOverheadDisplay(newName.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        RoombaControl roombaControl = GetComponent<RoombaControl>();
        RoombaKaboom roombaKaboom = GetComponentInChildren<RoombaKaboom>();

        if(IsOwner && !remainsSpawned && roombaControl.IsExploding && roombaKaboom.TimeElapsed >= roombaKaboom.HideRoombaAt)
        {
            remainsSpawned = true;
            SpawnPlayerRemainsServerRpc();
        }

        if (wasExplodingLastFrame && !roombaControl.IsExploding && IsOwner)
        {
            EndDie();
        }

        wasExplodingLastFrame = roombaControl.IsExploding;
    }

    [ServerRpc]
    public void BeginDieServerRpc(ServerRpcParams rpcParams = default)
    {
        BeginDieClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds } });
    }

    /// <summary>
    /// <para>Kills the player and awards the kill to the last attacker.</para>
    /// <para>Also increments this player's death counter and switches to class selection/respawn state.</para>
    /// </summary>
    [ClientRpc]
    public void BeginDieClientRpc(ClientRpcParams rpcParams = default)
    {
        // Should only be called once per death.
        if(IsDying)
        {
            return;
        }

        IsDying = true;

        RoombaControl owner = GetComponent<RoombaControl>();
        if (!owner.IsExploding)
        {
            owner.IsExploding = true;
            owner.ExplosionFxTimer = owner.ExplosionFxTime;

            // Create an inflating sphere to act as an explosion trigger.
            GameObject explosionRadius = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            explosionRadius.transform.position = owner.transform.position;
            explosionRadius.tag = "ExplosionRadius";

            Destroy(explosionRadius.GetComponent<MeshRenderer>());
            SphereCollider explosionCollider = explosionRadius.GetComponent<SphereCollider>();
            explosionCollider.radius = 0f;
            explosionCollider.isTrigger = true;

            TemporalInflater inflater = explosionRadius.AddComponent<TemporalInflater>();
            inflater.ApplyFunc = () => explosionRadius.GetComponent<SphereCollider>().radius = inflater.CurrentValue;
            inflater.TargetValue = 3.5f;
            inflater.EndTime = 1f;
        }

        // Make sure the health is kept below 0. Technically unnecessary, but I'm paranoid...
        health = -1;

        // If another player killed us, call the server RPC to give them a kill.
        Debug.Log($"Last attacker was {LastAttackerId}");

    }

    public void EndDie()
    {
        // Make sure we can find a GameManager for this ID.
        if (LastAttackerId != null && GameManager.FromId(LastAttackerId.Value) != null)
        {
            GameManager.Singleton.GiveScoreKillsServerRpc(LastAttackerId.Value, OwnerClientId);
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
    /// Spawns a "remains" object for a killed player.
    /// </summary>
    /// <param name="rpcParams"></param>
    [ServerRpc]
    public void SpawnPlayerRemainsServerRpc(ServerRpcParams rpcParams = default)
    {
        if (PlayerRemainsPrefab)
        {
            GameObject remains = Instantiate(PlayerRemainsPrefab, transform.position, transform.rotation);
            CollectableMess messScript = remains.GetComponent<CollectableMess>();
            messScript.IsPlayerCorpse = true;
            messScript.DestroyedRoombaId = OwnerClientId;

            Physics.IgnoreCollision(GetComponent<RoombaControl>().RoombaCollider, remains.GetComponent<Collider>(), true);
            remains.GetComponent<NetworkObject>().Spawn();
        }
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
