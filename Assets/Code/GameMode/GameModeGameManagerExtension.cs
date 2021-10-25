using Unity.Netcode;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Client-side gameplay logic for this gamemode. Should implement any networked events required by the gamemode.
/// </summary>
public abstract class GameModeGameManagerExtension : NetworkBehaviour
{
    /// <summary>
    /// The <see cref="GameManager"/> this script is part of.
    /// </summary>
    public GameManager Owner;

    /// <summary>
    /// <para>A copy of all player's most up-to-date scores.</para>
    /// <para>These are not synchronised automatically in <see cref="LobbyManager"/>, but rather the sync is triggered by each player's script.</para>
    /// </summary>
    public Dictionary<string, PlayerScore> PlayerScores;

    /// <summary>
    /// Networked event handlers.
    /// </summary>
    public Dictionary<string, CustomMessagingManager.HandleNamedMessageDelegate> EventHandlers = new Dictionary<string, CustomMessagingManager.HandleNamedMessageDelegate>();

    /// <summary>
    /// UI text object for health display.
    /// </summary>
    public Text healthText;

    /// <summary>
    /// UI text objects for the game over screen.
    /// </summary>
    public Text gameOverMainText, gameOverSubText;

    /// <summary>
    /// Values to fill the Game Over screen with.
    /// </summary>
    protected (string MainText, string SubText) GameOverAlert = ("", "");

    /// <summary>
    /// Initiates a gamemode in the client's gamemanager.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="target"></param>
    /// <returns></returns>
    public static T Create<T>(GameManager target) where T : GameModeGameManagerExtension
    {
        T extension = target.gameObject.AddComponent<T>();
        extension.Owner = target;
        target.GameModeExtensions = extension;
        return extension;
    }

    /// <summary>
    /// Initiates a gamemode in the client's gamemanager.
    /// </summary>
    /// <param name="extensionType"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static GameModeGameManagerExtension Create(Type extensionType, GameManager target)
    {
        GameModeGameManagerExtension extension = target.gameObject.AddComponent(extensionType) as GameModeGameManagerExtension;

        extension.Owner = target;
        target.GameModeExtensions = extension;
        return extension;
    }

    /// <summary>
    /// Removes the extension.
    /// </summary>
    /// <param name="existingExtension">Extension to be removed.</param>
    public static void Terminate(GameModeGameManagerExtension existingExtension)
    {
        Destroy(existingExtension);
    }

    /// <summary>
    /// Removes the extension.
    /// </summary>
    public void Terminate()
    {
        Destroy(this);
    }

    /// <summary>
    /// Carries out any UI initialisation steps (finding UI objects in the scene etc.)
    /// </summary>
    public virtual void InitialiseUI()
    {
        // Find the UI text objects in the scene.
        foreach (Text text in GameObject.Find("HUD").GetComponentsInChildren<Text>())
        {
            switch (text.name)
            {
                case "Health":
                    healthText = text;
                    break;
                case "GameOverAlert_Main":
                    gameOverMainText = text;
                    break;
                case "GameOverAlert_Sub":
                    gameOverSubText = text;
                    break;
            }

            if (healthText && gameOverMainText && gameOverSubText)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Assigns any networked event handlers.
    /// </summary>
    protected abstract void AssignEventHandlers();

    /// <summary>
    /// Registers networked event handlers from <see cref="AssignEventHandlers"/> with the Netcode API.
    /// </summary>
    private void RegisterEventHandlers()
    {
        foreach(KeyValuePair<string, CustomMessagingManager.HandleNamedMessageDelegate> eventHandler in EventHandlers)
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(eventHandler.Key, eventHandler.Value);
        }
    }

    protected virtual void Start()
    {
        PlayerScores = new Dictionary<string, PlayerScore>();

        if (IsOwner)
        {
            InitialiseUI();
            AssignEventHandlers();
            RegisterEventHandlers();
        }
    }

    protected virtual void Update()
    {
        if (IsOwner)
        {
            UpdateHud();
        }
    }

    /// <summary>
    /// Updates the HUD logic/values every frame.
    /// </summary>
    protected virtual void UpdateHud()
    {
        if (Owner?.SpawnedPlayer != null && Owner.SpawnedPlayer.PlayerControlled && PlayerStats.Local != null)
        {
            int health = PlayerStats.Local.health;

            if (health <= 0)
            {
                healthText.text = "";
                PlayerStats.Local.Die();
            }

            if (healthText != null)
            {
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
            else
            {
                Debug.Log("healthText was null!");
            }
        }
    }
}
