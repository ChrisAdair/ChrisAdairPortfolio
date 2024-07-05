using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Barebones.MasterServer;

public class RaidUI : MonoBehaviour
{
    public RaidLobbyUI raidLobbyUI;
    public JoinedLobby raidLobby;
    public Button JoinButton;
    public TextMeshProUGUI RaidDescription;
    public LeaderboardUI leaderboard;

    //Database variables for Raid level creation
    private int maxPlayers;
    private string levelName;
    private string sceneName;
    private string fileName;
    private int levelID;

    //Database variables for Raid information creation

    //Database variables for Raid highscore creation

    void Start()
    {
        //Setup all needed variables and room conditions
        Setup();
        //Setup the UI inputs after the variables are in place
        JoinButton.onClick.AddListener(delegate { StartCoroutine(JoinRaid()); });

        if(Msf.Client.Lobbies.LastJoinedLobby != null)
        {
            Msf.Client.Lobbies.LastJoinedLobby.Leave();
        }
    }

    /// <summary>
    /// Sets up the raid information, and updates the high score list
    /// </summary>
    public void Setup()
    {
        //Setup all of the current raid data from the database
        //TODO: Need function that can get a raid level number from the database based on schedule
        int raidLevelID = 1;
        Msf.Client.SaveLoad.GetRaidLevel(raidLevelID, (info, error) =>
        {
            fileName = info.levelData.fileName;
            sceneName = "Raid_Puzzle";
            levelID = info.levelData.levelID;
            levelName = info.levelData.levelName;
            maxPlayers = info.levelData.grainNumber;
            RaidDescription.text = info.description;
            leaderboard.levelID = levelID;
            leaderboard.SetUpMonthlyLeaderboard();
        });

    }
    /// <summary>
    /// Places the player in the public queue for a raid lobby
    /// </summary>
    public IEnumerator JoinRaid()
    {
        JoinButton.interactable = false;
        var loadingPromise = Msf.Events.FireWithPromise(Msf.EventNames.ShowLoading, "Finding a game ...");
        List<GameInfoPacket> gameList = null;
        bool completed = false;
        Msf.Client.Matchmaker.FindGames(games =>
        {
            gameList = games;
            completed = true;
        });
        bool foundLobby = false;

        yield return new WaitUntil(() => { return completed == true; });

        foreach (GameInfoPacket game in gameList)
        {
            if (game.Type != GameInfoType.Lobby)
            {
                continue;
            }

            if (game.OnlinePlayers >= game.MaxPlayers)
            {
                continue;
            }

            completed = false;
            Msf.Client.Lobbies.JoinLobby(game.Id, (lobby, error) =>
            {
                if (lobby == null)
                {
                    Debug.LogError("Error connecting to lobby: " + error);

                }
                else
                {
                    loadingPromise.Finish();
                    //Lobby is joined, start the RaidLobbyUI
                    raidLobby = lobby;
                    lobby.SetListener(raidLobbyUI);
                    foundLobby = true;
                }
                completed = true;
            });

            yield return new WaitUntil(() => { return completed == true; });

            if (foundLobby)
            {
                break;
            }
        }

        if (!foundLobby)
        {
            //Creates a standard room with the properties from the database
            var props = new Dictionary<string, string>()
                {
                    {MsfDictKeys.MaxPlayers, maxPlayers.ToString()},
                    {MsfDictKeys.RoomName, name},
                    {MsfDictKeys.MapName, levelName},
                    {MsfDictKeys.SceneName, sceneName},
                    {"GameStructurePath", fileName },
                    {"LevelID", levelID.ToString() },
                    {"ZoneID", "4" }
                };

            Msf.Client.Lobbies.CreateAndJoin("RaidLobby", props, (lobby, error) =>
            {
                if (lobby == null)
                {
                    Debug.LogError("Error connecting to lobby: " + error);

                }
                loadingPromise.Finish();
                //Lobby is joined, start the RaidLobbyUI
                raidLobby = lobby;
                lobby.SetListener(raidLobbyUI);
            });
        }
    }
    /// <summary>
    /// Creates a private Raid lobby that the master can invite players to
    /// </summary>
    public void CreatePrivateRoom()
    {

    }
}
