using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public partial class DeathMatchGameMode : MultiplayerGameMode
{
    public override string Name { get => "Deathmatch"; }

    private DateTime? MatchOverTime = null;

    public int MatchSeconds = 60 * 5;

    public int MaxScore = 50;

    public bool IsInProgress = true;

    protected override void HandleEvent(GameModeEvent gmEvent)
    {
        if (gmEvent is DeathMatchEvents.PlayerKilledEvent)
        {
            DeathMatchEvents.PlayerKilledEvent playerKilledEvent = gmEvent as DeathMatchEvents.PlayerKilledEvent;

            // Send a kill feed update.
            using(FastBufferWriter writer = new FastBufferWriter(sizeof(ulong) * 2, Unity.Collections.Allocator.Temp))
            {
                // Write player IDs to the buffer.
                writer.WriteValueSafe(playerKilledEvent.VictimId);
                writer.WriteValueSafe(playerKilledEvent.KillerId);

                // Send the update as a named message to all clients.
                InvokeGlobalEvent(DeathMatchGameManagerExtensions.KillFeedMessageHandlerName, writer);
            }
        }
    }

    public override Type GetExtensionsType() => typeof(DeathMatchGameManagerExtensions);

    public void Start()
    {
        MatchOverTime = DateTime.UtcNow + TimeSpan.FromSeconds(MatchSeconds);
        IsInProgress = true;
    }

    public override void Update()
    {
        base.Update();

        if (IsInProgress)
        {
            CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        bool outOfTime = CheckOutOfTime();
        (bool scoreReached, GameManager winner) = CheckScoreReached();

        if(outOfTime || scoreReached)
        {
            IsInProgress = false;

            using (FastBufferWriter writer = new FastBufferWriter(sizeof(bool) * 2 + sizeof(ulong), Unity.Collections.Allocator.Temp))
            {
                writer.WriteValueSafe(scoreReached);
                writer.WriteValueSafe(winner != null);
                if (winner != null)
                {
                    writer.WriteValueSafe(winner.OwnerClientId);
                }

                InvokeGlobalEvent(DeathMatchGameManagerExtensions.GameOverMessageHandlerName, writer);
            }
        }
    }

    private bool CheckOutOfTime()
    {
        return DateTime.UtcNow >= MatchOverTime;
    }

    private (bool scoreReached, GameManager winner) CheckScoreReached()
    {
        GameManager leader = null;
        bool scoreReached = false;

        foreach(GameManager player in players.Values)
        {
            if(player.Score.TotalPoints >= MaxScore)
            {
                scoreReached = true;
            }

            // Find player with most points.
            if (leader == null || player.Score.TotalPoints > leader.Score.TotalPoints)
            {
                leader = player;
            }
        }

        return (scoreReached, leader);
    }
}
