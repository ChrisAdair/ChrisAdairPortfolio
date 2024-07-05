using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Barebones.Networking;
using Barebones.MasterServer;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour {

    [Header("Set in inspector")]
    public int zoneID;
    

    [Header("Set Dynamically")]
    public string levelName;
    public string fileName;
    public string sceneName;
    public string description;
    public int levelID;
    public bool singlePlayer = true;
    public CreateGameProgressUi progressUi;
    public TextMeshProUGUI buttonName;
    public Button button;
    public GameObject singleplayerLevelProps;
    public LevelDescription levelDescription;
    public Button triggeredCutscene;

    private LevelPropsPacket levelProps;
    private AsyncOperation loading;

	void Start () {
        if(button == null)
            button = GetComponent<Button>();
        buttonName = GetComponentInChildren<TextMeshProUGUI>();
        
	}
	
    public void SetButtonProperties()
    {
        Msf.Client.SaveLoad.GetLevelProperties(levelID, (packet, error) =>
        {
            if (packet == null)
            {
                Debug.LogError("Error retrieving level properties: " + error);
            }
            levelProps = packet;
            
            if (singlePlayer)
            {
                button.onClick.AddListener(delegate { levelDescription.SetInformation(description, delegate { StartCoroutine(OnStartLevelSingleplayer()); }); });
                //Change the music to the level's song
                button.onClick.AddListener(delegate { levelDescription.zoneID = zoneID; });
            }
            else
            {
                button.onClick.AddListener(delegate { levelDescription.SetInformation(description, OnStartLevelMultiplayer); });
            }

        });
        
    }

    private void OnStartLevelMultiplayer()
    {
        if (progressUi == null)
        {
            Logs.Error("You need to set a ProgressUi");
            return;
        }

        if (!Msf.Client.Auth.IsLoggedIn)
        {
            Debug.LogError("You must be logged in to create a room");
            return;
        }

        var name = levelName;

        var maxPlayers = 4;

        var settings = new Dictionary<string, string>
            {
                {MsfDictKeys.MaxPlayers, maxPlayers.ToString()},
                {MsfDictKeys.RoomName, name},
                {MsfDictKeys.MapName, levelName},
                {MsfDictKeys.SceneName, sceneName},
                {"GameStructurePath", fileName },
                {"LevelID", levelID.ToString() },
                {"ZoneID", zoneID.ToString() }

            };

        Msf.Client.Spawners.RequestSpawn(settings, "", (requestController, errorMsg) =>
        {
            if (requestController == null)
            {
                progressUi.gameObject.SetActive(false);
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateError("Failed to create a game: " + errorMsg));

                Logs.Error("Failed to create a game: " + errorMsg);
            }
            if(Msf.Client.Team.team != null)
            {
                Msf.Client.Team.StartGame(requestController, zoneID);
            }
            else
            {
                progressUi.Display(requestController);
            }
            
        });
    }
    private IEnumerator OnStartLevelSingleplayer()
    {
        var name = levelName;

        var maxPlayers = 1;

        var settings = new Dictionary<string, string>
        {
            {MsfDictKeys.MaxPlayers, maxPlayers.ToString()},
            {MsfDictKeys.RoomName, name},
            {MsfDictKeys.MapName, levelName},
            {MsfDictKeys.SceneName, sceneName},
            {"GameStructurePath", fileName },
            {"LevelID", levelID.ToString() },
            {"Mode", "Singleplayer" },
            {"ZoneID", zoneID.ToString() }

        };

        SingleplayerDataStruct props = Instantiate(singleplayerLevelProps).GetComponent<SingleplayerDataStruct>();
        props.properties = settings;
        props.Initialize();
        //Wait a frame for the data to initialize itself
        LoadingScreen.singleton.GetDatabaseHint();
        yield return new WaitUntil(()=> { return Msf.Client.SaveLoad.complete; }); 
        loading =  SceneManager.LoadSceneAsync(sceneName);
        props.sceneLoading = loading;
        
        loading.completed += (loading) => { LoadingScreen.singleton.fade = false; };
        while(!loading.isDone)
        {
            LoadingScreen.singleton.progress = loading.progress;
            LoadingScreen.singleton.fade = true;
            yield return null;
        }
    }
}
