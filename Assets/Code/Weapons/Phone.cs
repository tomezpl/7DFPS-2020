using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Phone : Weapon
{
    /// <summary>
    /// The player object this weapon belongs to.
    /// </summary>
    public PlayerStats Owner;

    /// <summary>
    /// The radius of the explosion point light(s).
    /// </summary>
    public float ExplosionLightRadius = 1.5f;

    /// <summary>
    /// The explosion point light(s).
    /// </summary>
    public Light[] ExplosionLights;

    /// <summary>
    /// The 3D text on the phone screen with the bomb timer.
    /// </summary>
    public TextMeshPro TimerText;

    /// <summary>
    /// Damage taken being right next to the explosion.
    /// </summary>
    public int CloseUpDamage = 200;

    /// <summary>
    /// Damage radius - players this far away will not receive any damage from the explosion.
    /// </summary>
    public float DamageRadius = 3.5f;

    /// <summary>
    /// The duration of the explosion effect (in seconds).
    /// </summary>
    public float ExplosionFxTime = 1f;

    /// <summary>
    /// Is the phone currently in process of detonation?
    /// </summary>
    public bool isExploding = false;

    /// <summary>
    /// Timer that will count until <see cref="ExplosionFxTime"/>.
    /// </summary>
    public float detonationTimer = 0f;

    /// <summary>
    /// <para>Time before battery detonation.</para>
    /// <para>This is usually how much time the player gets from spawn till they blow up.</para>
    /// </summary>
    public double TimerLength = 20;

    /// <summary>
    /// The absolute timestamp when detonation should occur.
    /// </summary>
    DateTime detonationTime;

    // Start is called before the first frame update
    void Start()
    {
        // Set the bomb timer.
        detonationTime = DateTime.Now + TimeSpan.FromSeconds(TimerLength);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isExploding && (DateTime.Now > detonationTime || Input.GetButtonDown("Fire1")) && IsMine)
        {
            DetonateLithiumBombServerRpc();
        }

        // Update the 3D timer text on the phone screen.
        if (detonationTime != null)
        {
            if (detonationTime > DateTime.Now)
            {
                if (TimerText)
                {
                    TimerText.text = (detonationTime - DateTime.Now).ToString("ss");
                }
            }
            else
            {
                TimerText.text = "00";
            }
        }

        if (isExploding)
        {
            // Update the detonation timer.
            detonationTimer -= Time.deltaTime;

            // Check if detonation has finished.
            // TODO: DealDamage could be moved out of this to occur sooner than once the full effect has finished.
            if (detonationTimer <= 0f)
            {
                isExploding = false;

                if (IsMine)
                {
                    foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
                    {
                        if (player.GetComponent<RoombaControl>().PlayerControlled)
                        {
                            // Don't bother damaging ourseleves as the explosion is a suicide anyway.
                            continue;
                        }

                        // Damage to deal to this player, with distance taken into account.
                        float dmgMult = Mathf.InverseLerp(DamageRadius, 0f, Vector3.Distance(player.transform.position, transform.position));

                        Debug.Log($"Dealing {Mathf.RoundToInt(dmgMult * CloseUpDamage)} to Player {player.GetComponent<RoombaControl>()?.OwnerClientId}");

                        // Damage players in the area.
                        // TODO: Players too far away to receive damage could be filtered out before sending RPCs to reduce network traffic.
                        Owner.GetComponent<RoombaControl>().DealDamageServerRpc(Mathf.RoundToInt(dmgMult * CloseUpDamage), player.GetComponent<RoombaControl>().OwnerClientId);
                    }

                    // Send a DealDamage RPC to update LastAttackerId.
                    Owner.GetComponent<RoombaControl>().DealDamageServerRpc(CloseUpDamage, NetworkManager.LocalClientId);
                }
            }
        }
    }

    /// <summary>
    /// Acknowledges the bomb detonation on the server and forwards it to clients.
    /// </summary>
    /// <param name="serverRpcParams"></param>
    [ServerRpc]
    public void DetonateLithiumBombServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Send RPC to all connected clients.
        DetonateLithiumBombClientRpc(serverRpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// Activates the bomb detonation effects on the clients.
    /// </summary>
    /// <param name="detonatorClientId">Client ID of the player blowing up.</param>
    /// <param name="clientRpcParams"></param>
    [ClientRpc]
    public void DetonateLithiumBombClientRpc(ulong detonatorClientId, ClientRpcParams clientRpcParams = default)
    {
        if (OwnerClientId == detonatorClientId)
        {
            Owner.GetComponent<RoombaControl>().LockInput = true;
            isExploding = true;
            detonationTimer = ExplosionFxTime;
        }
    }
}
