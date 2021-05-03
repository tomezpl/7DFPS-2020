using MLAPI.Messaging;
using UnityEngine;

public partial class GameManager
{
    [ServerRpc]
    public void GiveScoreKillsServerRpc(ulong clientId, int killsToGive = 1, ServerRpcParams serverRpcParams = default)
    {
        GameManager killerManager = FromId(clientId);
        Debug.Log($"Giving client {clientId} {killsToGive} kills");
        PlayerScore score = killerManager.Score;
        score.Kills += killsToGive;
        killerManager.SerializedScore.Value = score.ToString();
    }

    [ServerRpc]
    public void GiveScoreDeathsServerRpc(ulong clientId, int deathsToGive = 1, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"Giving client {clientId} {deathsToGive} deaths");
        PlayerScore score = Score;
        score.Deaths += deathsToGive;
        SerializedScore.Value = score.ToString();
    }
}