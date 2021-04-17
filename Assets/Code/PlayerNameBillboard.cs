using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Script to point all player overhead names to be facing towards the local player's camera.
/// </summary>
public class PlayerNameBillboard : MonoBehaviour
{
    /// <summary>
    /// Alias for <see cref="LobbyManager.Singleton"/>.
    /// </summary>
    LobbyManager lobby { get => LobbyManager.Singleton; }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(lobby?.LocalPlayerObject)
        {
            // Find all players.
            // TODO: Could use the player stats dictionary from LobbyManager to cache this.
            foreach(GameObject obj in GameObject.FindGameObjectsWithTag("Player"))
            {
                foreach(TextMeshPro tmp in obj.GetComponentsInChildren<TextMeshPro>())
                {
                    // Find each player's overhead name text.
                    if (tmp.name == "PlayerName")
                    {
                        // Point it at the local player's camera.
                        tmp.transform.LookAt(lobby.LocalPlayerObject.transform);
                        tmp.transform.Rotate(0f, 180f, 0f, Space.Self);
                        break;
                    }
                }
            }
        }
    }
}
