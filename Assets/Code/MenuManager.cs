using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public string PlayerName = "Player";
    public string Server = $"{MultiplayerInfo.DefaultIpAddress}:{MultiplayerInfo.DefaultPort}";

    public bool IsHost = true;
    public int MapIndex = 0;

    public string[] MapTitles = new string[1];
    public int[] MapIndices = new int[1];
    public string[] MapDescriptions = new string[1];
    public Sprite[] MapPreviews = new Sprite[1];

    public Image MapPreview;
    public TMPro.TMP_Text MapTitle;
    public TMPro.TMP_Text MapDesc;

    public GameObject MapSelectionScreen;
    public TMPro.TMP_Dropdown MapDropdown;

    public GameObject MultiplayerScreen;

    public GameObject MainScreen;

    // Start is called before the first frame update
    void Start()
    {
        MapIndex = MapIndices[0];
        MapDropdown.options = MapTitles.Select(title => new TMPro.TMP_Dropdown.OptionData(title)).ToList();
        UpdateMapPreview();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ReadMapSelection()
    {
        MapIndex = MapIndices[MapDropdown.value];

        UpdateMapPreview();
    }

    public void UpdateMapPreview()
    {
        MapPreview.sprite = MapPreviews[MapDropdown.value];
        MapDesc.text = MapDescriptions[MapDropdown.value];
        MapTitle.text = MapTitles[MapDropdown.value];
    }

    public void ShowMaps()
    {
        MapSelectionScreen.SetActive(true);
        MultiplayerScreen.SetActive(true);

        MainScreen.SetActive(false);
    }

    public void ShowMainScreen()
    {
        MapSelectionScreen.SetActive(false);
        MultiplayerScreen.SetActive(false);

        MainScreen.SetActive(true);
    }

    public void LoadMapAsHost()
    {
        IsHost = true;
        LoadMap();
    }

    public void LoadMapAsClient()
    {
        IsHost = false;
        LoadMap();
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
