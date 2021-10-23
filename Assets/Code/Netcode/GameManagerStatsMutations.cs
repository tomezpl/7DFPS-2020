using Unity.Netcode;
using UnityEngine;

public partial class GameManager
{
    /// <summary>
    /// Grants kill(s) to the player specified by their client ID.
    /// </summary>
    /// <param name="clientId">The client ID of the player to be granted a kill.</param>
    /// <param name="killsToGive">Number of kills to give. 1 by default.</param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc]
    public void GiveScoreKillsServerRpc(ulong clientId, ulong victimId, int killsToGive = 1, ServerRpcParams serverRpcParams = default)
    {
        // Make sure the kill wasn't a self-kill (self-kills shouldn't count as kill points)
        if (clientId != victimId)
        {
            GameManager killerManager = FromId(clientId);
            Debug.Log($"Giving client {clientId} {killsToGive} kills");
            PlayerScore score = killerManager.Score;
            score.Kills += killsToGive;
            killerManager.SerializedScore.Value = score.ToString();
        }

        // Notify the gamemode with an event.
        LobbyManager.Singleton.CurrentGameMode.EmitEvent(new DeathMatchEvents.PlayerKilledEvent { KillerId = clientId, VictimId = victimId });
    }

    /// <summary>
    /// Adds death(s) to the player score of a specified client.
    /// </summary>
    /// <param name="clientId">The client ID of the player we want to assign death(s) to.</param>
    /// <param name="deathsToGive">Number of deaths to add. 1 by default.</param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc]
    public void GiveScoreDeathsServerRpc(ulong clientId, int deathsToGive = 1, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"Giving client {clientId} {deathsToGive} deaths");
        PlayerScore score = Score;
        score.Deaths += deathsToGive;
        SerializedScore.Value = score.ToString();
    }
}