using MLAPI.Messaging;
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
    public void GiveScoreKillsServerRpc(ulong clientId, int killsToGive = 1, ServerRpcParams serverRpcParams = default)
    {
        GameManager killerManager = FromId(clientId);
        Debug.Log($"Giving client {clientId} {killsToGive} kills");
        PlayerScore score = killerManager.Score;
        score.Kills += killsToGive;
        killerManager.SerializedScore.Value = score.ToString();
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