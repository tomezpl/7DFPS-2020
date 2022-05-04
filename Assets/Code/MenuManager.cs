using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public string PlayerName = "Player";
    public string Server = $"{MultiplayerInfo.DefaultIpAddress}:{MultiplayerInfo.DefaultPort}";

    public bool IsHost = true;
    public int MapIndex = 0;

    public string[] MapTitles = new string[1];
    public int[] MapIndices = new int[1];

    // Start is called before the first frame update
    void Start()
    {
        MapIndex = MapIndices[0];
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadMap()
    {
        if (MapIndex != SceneManager.GetActiveScene().buildIndex)
        {
            DataStore.SetMultiplayerSettings(PlayerName, Server, IsHost);
            SceneManager.LoadScene(MapIndex);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
