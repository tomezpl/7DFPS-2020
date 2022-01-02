using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side logic for the Deathmatch gamemode.
/// </summary>
public partial class DeathMatchGameMode : MultiplayerGameMode
{
    public override string Name { get => "Deathmatch"; }

    /// <summary>
    /// When should the game end?
    /// </summary>
    private DateTime? MatchOverTime = null;

    /// <summary>
    /// How long (in seconds) should the match take?
    /// </summary>
    public int MatchSeconds = 60 * 5;

    /// <summary>
    /// How many points should a player have to reach to win the game?
    /// </summary>
    public int MaxScore = 50;

    /// <summary>
    /// Is the match still in progress?
    /// </summary>
    public bool IsInProgress = true;

    /// <summary>
    /// Timer updated with deltaTime to emit timer updates to clients approx. every second.
    /// </summary>
    private float secondCounter = 0f;

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

        secondCounter += Time.deltaTime;

        if (IsInProgress)
        {
            CheckGameOver();

            // Update the time on all clients every second.
            if(secondCounter >= 1f)
            {
                UpdateTimerForPlayers();
                secondCounter = 0f;
            }
        }
    }

    /// <summary>
    /// Stops the game if a gameover/win condition has been reached.
    /// </summary>
    private void CheckGameOver()
    {
        // Check time first.
        bool outOfTime = CheckOutOfTime();
        // Check if any of the players reached max score.
        (bool scoreReached, GameManager winner) = CheckScoreReached();

        if(outOfTime || scoreReached)
        {
            // Stop the game.
            IsInProgress = false;

            using (FastBufferWriter writer = new FastBufferWriter(sizeof(bool) * 2 + sizeof(ulong), Unity.Collections.Allocator.Temp))
            {
                writer.WriteValueSafe(scoreReached);
                writer.WriteValueSafe(winner != null);
                if (winner != null)
                {
                    writer.WriteValueSafe(winner.OwnerClientId);
                }

                // Notify all players.
                InvokeGlobalEvent(DeathMatchGameManagerExtensions.GameOverMessageHandlerName, writer);
            }
        }
    }

    /// <summary>
    /// Checks if max match duration has passed.
    /// </summary>
    /// <returns>true if current time is past <see cref="MatchOverTime"/>.</returns>
    private bool CheckOutOfTime()
    {
        return DateTime.UtcNow >= MatchOverTime;
    }

    /// <summary>
    /// Checks if max score has been reached.
    /// </summary>
    /// <returns>scoreReached is true if max score reached, otherwise false. winner is the player with most points (regardless of scoreReached).</returns>
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

    /// <summary>
    /// Sync current remaining time for all players.
    /// </summary>
    private void UpdateTimerForPlayers()
    {
        using (FastBufferWriter writer = new FastBufferWriter(sizeof(long), Unity.Collections.Allocator.Temp))
        {
            writer.WriteValueSafe((MatchOverTime - DateTime.UtcNow).Value.Ticks);
            InvokeGlobalEvent(DeathMatchGameManagerExtensions.TimerUpdateMessageName, writer);
        }
    }
}
