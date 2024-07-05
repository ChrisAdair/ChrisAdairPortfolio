using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Barebones.MasterServer;
using UnityEngine.SceneManagement;

public class LevelSelectLabUI : MonoBehaviour
{
    public enum LevelType
    {
        Competitive,
        Challenge
    }
    public GameObject gameTemplate;
    public GameObject content;
    public List<LevelSelectTemplate> levels;
    public Button startGameButton;
    public LeaderboardUI leaderboard;
    public LevelType selectionType;

    public LevelSelectTemplate selectedGame = null;
    public GameObject singlePlayerLevelProps;

    private AsyncOperation loading;
    void Start()
    {
        levels = new List<LevelSelectTemplate>();
        switch(selectionType)
        {
            case LevelType.Challenge:
                Msf.Client.SaveLoad.GetNumberOfChalLevels(numLevels =>
                {
                    for (int i = 1; i <= numLevels; i++)
                    {
                        Msf.Client.SaveLoad.GetChalLevel(i, (packet, error) =>
                        {
                            GameObject game = Instantiate(gameTemplate, content.transform);
                            var gameData = game.GetComponent<LevelSelectTemplate>();
                            gameData.description.text = packet.description;
                            gameData.levelName.text = packet.levelData.levelName;
                            gameData.levelSelect = this;
                            gameData.props = packet.levelProps;
                            gameData.level = packet.levelData;
                            levels.Add(gameData);
                        });
                    }
                });
                break;
            case LevelType.Competitive:
                Msf.Client.SaveLoad.GetNumberOfCompLevels(numLevels =>
                {
                    for (int i = 1; i <= numLevels; i++)
                    {
                        Msf.Client.SaveLoad.GetCompLevel(i, (packet, error) =>
                        {
                            GameObject game = Instantiate(gameTemplate, content.transform);
                            var gameData = game.GetComponent<LevelSelectTemplate>();
                            gameData.description.text = packet.description;
                            gameData.levelName.text = packet.levelData.levelName;
                            gameData.levelSelect = this;
                            gameData.props = packet.levelProps;
                            gameData.level = packet.levelData;
                            levels.Add(gameData);
                        });
                    }
                });
                break;
        }
        
        startGameButton.onClick.AddListener(StartGame);
        startGameButton.interactable = false;
    }

    public void SelectLevel(LevelSelectTemplate level)
    {
        foreach(LevelSelectTemplate lv in levels)
        {
            lv.selected = !lv.selected && lv == level;
        }
        UpdateStartButton();
    }

    public void UpdateStartButton()
    {
        foreach (LevelSelectTemplate lv in levels)
        {
            if (lv.selected)
            {
                selectedGame = lv;
                startGameButton.interactable = true;
                break;
            }
            else
            {
                startGameButton.interactable = false;
            }
        }
        
    }
    public void UpdateLeaderboard(int levelID)
    {
        if(leaderboard != null)
        {
            leaderboard.levelID = levelID;
            leaderboard.SetUpDailyComp();
        }
    }
    public void StartGame()
    {
        StartCoroutine(OnStartLevelSingleplayer());
    }

    private IEnumerator OnStartLevelSingleplayer()
    {
        var name = selectedGame.level.levelName;

        var maxPlayers = 1;

        var settings = new Dictionary<string, string>
        {
            {MsfDictKeys.MaxPlayers, maxPlayers.ToString()},
            {MsfDictKeys.RoomName, name},
            {MsfDictKeys.MapName, selectedGame.level.levelName},
            {MsfDictKeys.SceneName, "Raid_Puzzle"},
            {"GameStructurePath", selectedGame.level.fileName },
            {"LevelID", selectedGame.level.levelID.ToString() },
            {"Mode", GetMode() },
            {"ZoneID", "4" }

        };

        SingleplayerDataStruct props = Instantiate(singlePlayerLevelProps).GetComponent<SingleplayerDataStruct>();
        props.properties = settings;
        props.Initialize();
        //Wait a frame for the data to initialize itself
        yield return null;
        LoadingScreen.singleton.GetDatabaseHint();
        yield return new WaitUntil(() => { return Msf.Client.SaveLoad.complete; });
        loading = SceneManager.LoadSceneAsync("Raid_Puzzle");
        props.sceneLoading = loading;

        loading.completed += (loading) => { LoadingScreen.singleton.fade = false; };
        while (!loading.isDone)
        {
            LoadingScreen.singleton.progress = loading.progress;
            LoadingScreen.singleton.fade = true;
            yield return null;
        }
    }
    private string GetMode()
    {
        switch(selectionType)
        {
            case LevelType.Challenge:
                return "Challenge";
            case LevelType.Competitive:
                return "Competitive";
        }
        return "Challenge";
    }
}
