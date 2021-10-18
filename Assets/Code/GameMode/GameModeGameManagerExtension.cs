using MLAPI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class GameModeGameManagerExtension : NetworkBehaviour
{
    public GameManager Owner;

    /// <summary>
    /// <para>A copy of all player's most up-to-date scores.</para>
    /// <para>These are not synchronised automatically in <see cref="LobbyManager"/>, but rather the sync is triggered by each player's script.</para>
    /// </summary>
    public Dictionary<string, PlayerScore> PlayerScores;

    /// <summary>
    /// UI text object for health display.
    /// </summary>
    public Text healthText;

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

    public static void Terminate(GameModeGameManagerExtension existingExtension)
    {
        Destroy(existingExtension);
    }

    public void Terminate()
    {
        Destroy(this);
    }

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
            }

            if (healthText)
            {
                break;
            }
        }
    }

    protected virtual void Start()
    {
        InitialiseUI();

        PlayerScores = new Dictionary<string, PlayerScore>();
    }

    protected virtual void Update()
    {
        UpdateHud();
    }

    protected virtual void UpdateHud()
    {
        if (Owner?.SpawnedPlayer && Owner.SpawnedPlayer.PlayerControlled && PlayerStats.Local)
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
        }
    }
}
