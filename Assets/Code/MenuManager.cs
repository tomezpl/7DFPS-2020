using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    public string PlayerName = "Player";
    public string Server = $"{MultiplayerInfo.DefaultIpAddress}:{MultiplayerInfo.DefaultPort}";

    public bool IsHost = true;
    public int MapIndex = 0;

    public string[] MapTitles = new string[1];
    public int[] MapSceneIndices = new int[1];
    public string[] MapDescriptions = new string[1];
    public Sprite[] MapPreviews = new Sprite[1];

    [SerializeField]
    public UIDocument MainMenuUi;
    public bool UiInitialised;

    private bool isInLevelSelection = false;

    private void OnEnable()
    {
        TryInitialiseUi();
    }

    // Start is called before the first frame update
    void Start()
    {
        MapIndex = MapSceneIndices[0];
    }

    // Update is called once per frame
    void Update()
    {
        TryInitialiseUi();
    }

    void TryInitialiseUi()
    {
        if (!UiInitialised)
        {
            if (TryGetComponent(out MainMenuUi) && MainMenuUi.rootVisualElement != null)
            {
                UiInitialised = true;
                CloseBtnCallback(null);
                InitialiseUiControls();
                PreselectMap();
            }
        }
    }

    void PreselectMap()
    {
        MainMenuUi.rootVisualElement.Q<DropdownField>("LevelDropdown").value = MapTitles[MapIndex];
    }

    void InitialiseUiControls()
    {
        MainMenuUi.rootVisualElement.Q("CloseBtn").RegisterCallback<MouseCaptureEvent>(CloseBtnCallback);
        MainMenuUi.rootVisualElement.Q("PlayBtn").RegisterCallback<MouseCaptureEvent>(PlayBtnCallback);
        MainMenuUi.rootVisualElement.Q("AuxBtn").RegisterCallback<MouseCaptureEvent>(AuxBtnCallback);
        MainMenuUi.rootVisualElement.Q<DropdownField>("LevelDropdown").RegisterValueChangedCallback(LevelChangedCallback);

        MainMenuUi.rootVisualElement.Q<TextField>("ServerIp").value = $"{MultiplayerInfo.DefaultIpAddress}:{MultiplayerInfo.DefaultPort}";

        MainMenuUi.rootVisualElement.Q<TextField>("PlayerName").value = "Player";
    }

    /// <summary>
    /// Event handler for the "auxiliary" button - in the main screen it's the exit button, in the "play" screen it's the host game button.
    /// </summary>
    /// <param name="ev"></param>
    public void AuxBtnCallback(MouseCaptureEvent ev)
    {
        if(isInLevelSelection)
        {
            LoadMapAsHost();
        }
        else
        {
            Application.Quit();
        }
    }

    public void LevelChangedCallback(ChangeEvent<string> changeEv)
    {
        int newIndex = -1;
        for(int i = 0; i < MapTitles.Length; i++)
        {
            if(MapTitles[i] == changeEv.newValue)
            {
                newIndex = i;
            }
        }

        MapIndex = newIndex == -1 ? MapIndex : newIndex;

        UpdateMapPreview();
    }

    public void CloseBtnCallback(MouseCaptureEvent ev)
    {
        MainMenuUi.rootVisualElement.Q("LevelSelection").style.display = DisplayStyle.None;
        MainMenuUi.rootVisualElement.Q<TextField>("PlayerName").style.display = DisplayStyle.None;
        MainMenuUi.rootVisualElement.Q<TextField>("ServerIp").style.display = DisplayStyle.None;

        MainMenuUi.rootVisualElement.Q<Button>("PlayBtn").text = "PLAY";
        MainMenuUi.rootVisualElement.Q<Button>("PlayBtn").RemoveFromClassList("joinBtn");
        MainMenuUi.rootVisualElement.Q<Button>("AuxBtn").text = "EXIT";
        MainMenuUi.rootVisualElement.Q<Button>("AuxBtn").RemoveFromClassList("hostBtn");

        isInLevelSelection = false;
    }

    public void PlayBtnCallback(MouseCaptureEvent ev)
    {
        if (!isInLevelSelection)
        {
            MainMenuUi.rootVisualElement.Q("LevelSelection").style.display = DisplayStyle.Flex;
            MainMenuUi.rootVisualElement.Q<TextField>("PlayerName").style.display = DisplayStyle.Flex;
            MainMenuUi.rootVisualElement.Q<TextField>("ServerIp").style.display = DisplayStyle.Flex;

            MainMenuUi.rootVisualElement.Q<Button>("PlayBtn").text = "JOIN";
            MainMenuUi.rootVisualElement.Q<Button>("PlayBtn").AddToClassList("joinBtn");
            MainMenuUi.rootVisualElement.Q<Button>("AuxBtn").text = "HOST";
            MainMenuUi.rootVisualElement.Q<Button>("AuxBtn").AddToClassList("hostBtn");

            isInLevelSelection = true;
        }
        else
        {
            LoadMapAsClient();
        }
    }

    public void UpdateMapPreview()
    {
        MainMenuUi.rootVisualElement.Q<Label>("MapTitle").text = MapTitles[MapIndex];
        MainMenuUi.rootVisualElement.Q<Label>("MapDesc").text = MapDescriptions[MapIndex];
        MainMenuUi.rootVisualElement.Q<VisualElement>("MapPreview").style.backgroundImage = new StyleBackground(MapPreviews[MapIndex]);
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
            PlayerName = MainMenuUi.rootVisualElement.Q<TextField>("PlayerName").value;
            Server = MainMenuUi.rootVisualElement.Q<TextField>("ServerIp").value;

            DataStore.SetMultiplayerSettings(PlayerName, Server, IsHost);
            SceneManager.LoadScene(MapIndex);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
