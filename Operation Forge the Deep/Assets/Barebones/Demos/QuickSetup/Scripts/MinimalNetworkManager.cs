using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Barebones.MasterServer;
using System.IO;
using UnityEngine.UI;
using Barebones.Networking;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Rendering.PostProcessing;



public class MinimalNetworkManager : NetworkManager {

    [Header("Set in Inspector")]
    public UnetGameRoom GameRoom;
    public List<Material> allSkyboxes;
    public List<PostProcessVolume> allPostProc;
    [Tooltip("Enter in the different x,y,z positions for the background")]
    public List<Vector3> allCliffPositions;
    [Tooltip("Drag the parent object of the background here so it will be moved to the right spot")]
    public GameObject backgroundParentObject;
    [Tooltip("Drag the lighting objects for each level here")]
    public List<GameObject> individualLevelLighting;
    [Tooltip("Set the colors for the fog on each level")]
    public List<Color> levelFogColors;
    public List<Material> CubeMaterials;
    public List<GameObject> EuphoticEnable;
    public List<GameObject> EuphoticDisable;
    public List<GameObject> DysphoticEnable;
    public List<GameObject> DysphoticDisable;
    public List<GameObject> AphoticEnable;
    public List<GameObject> AphoticDisable;
    public List<GameObject> LabEnable;
    public List<GameObject> LabDisable;
    public List<float> fogEndDistance;
    public List<Material> highlightCubeMat;

    [Header("Set Dynamically")]
    public Skybox zoneSkybox;
    public StreamWriter writer;
    public string chatRoomName;
    public bool singleplayer;
    public bool completedFileStream = false;
    private SingleplayerDataStruct singleplayerData;
    private string thFilePathName;
    private string thFileStrippedName;
    private GrainNetworkAssigner assigner;


    private List<string> users;
    private int gameoverDatabasePostCount;
    private bool spawnedPlayer = false;

    //Set up singleton behavior
    public new static MinimalNetworkManager singleton;

    void OnEnable()
    {
        singleplayerData = FindObjectOfType<SingleplayerDataStruct>();
        singleplayer = singleplayerData != null;
        if (!singleplayer)
        {
            if (GameRoom == null)
            {
                Debug.LogError("Game Room property is not set on NetworkManager");
                return;
            }

            // Subscribe to events
            GameRoom.PlayerJoined += OnPlayerJoined;
            //GameRoom.PlayerLeft += OnPlayerLeft;
        }

        users = new List<string>();
        singleton = this;
        gameoverDatabasePostCount = 0;
        if (singleplayer)
        {
            //Start a host to emulate a singleplayer experience
            Destroy(GameRoom);
            autoCreatePlayer = true;
            SetZoneAssets(singleplayerData.properties["ZoneID"]);
            StartHost();
        }
        else
        {
            SetZoneAssets(GameRoom.ZoneID);
        }

    }

    private void OnPlayerJoined(UnetMsfPlayer player)
    {
        if (singleplayer)
            return;
        //If the grain network has not been created yet, create the grain network
        //Also create the single log file for the time history
        if (GameObject.FindGameObjectWithTag("GameController") == null)
        {
            chatRoomName = GameRoom?.ChatChannel;
            //Spawn the grain network assigner if it's not here
            GameObject networkAssigner = Instantiate(singleton.spawnPrefabs[3]);
            assigner = networkAssigner.GetComponent<GrainNetworkAssigner>();
            var structurePath = GameRoom?.GrainStructurePath;

            Logs.Info("StructurePath in NM : |" + structurePath + "|");

            if (assigner != null && (structurePath != null || structurePath != ""))
            {
#if UNITY_STANDALONE_WIN
                assigner.structure = SaveFunctions.LoadFile(Application.dataPath + @"/Save Files/" + structurePath + ".SAVE");
                string thLocation = Application.dataPath + @"/THData/";
                if (!Directory.Exists(thLocation))
                {
                    Directory.CreateDirectory(thLocation);
                }

                string sessionName = structurePath + " " + GameRoom?.MapName + " " + System.DateTime.UtcNow.ToString("d-M-yyyy-hh-mm");

                writer = File.CreateText(thLocation + sessionName + ".txt");
#else
                Debug.Log("Entered the following file: " + Application.dataPath + @"/Save Files/" + structurePath + ".SAVE");
                assigner.structure = SaveFunctions.LoadFile(Application.dataPath + @"/Save Files/" + structurePath + ".SAVE");
                string thLocation = Application.dataPath + @"/THData/";
                if (!Directory.Exists(thLocation))
                {
                    Directory.CreateDirectory(thLocation);
                }

                string sessionName = structurePath + " " + GameRoom?.MapName + " " + System.DateTime.UtcNow.ToString("d-M-yyyy-hh-mm");

                writer = File.CreateText(thLocation + sessionName + ".txt");
#endif

            }
            else
                assigner.structure = null;
            NetworkServer.Spawn(networkAssigner);

        }

        assigner = GameObject.FindGameObjectWithTag("GameController").GetComponent<GrainNetworkAssigner>();

        // Spawn the player object (https://docs.unity3d.com/Manual/UNetPlayers.html)
        var playerGameObject = Instantiate(playerPrefab);

        var playerController = playerGameObject.GetComponent<PlayerBehavior>();
        playerController.ChatChannel = chatRoomName;
        playerController.username = player.Username;
        playerController.levelName = GameRoom.MapName;
        SetPlayerCubeHighlightMat(playerController, GameRoom.ZoneID);
        NetworkServer.AddPlayerForConnection(player.Connection, playerGameObject, 0);

        if (GameObject.FindGameObjectWithTag("GameCompletion") == null)
        {
            GameObject completion = Instantiate(singleton.spawnPrefabs[5]);
            //Fill in the needed level completion values here
            if (!singleplayer)
            {
                if (SceneManager.GetActiveScene().name == "Raid_Puzzle")
                {
                    Msf.Server.Save.GetRaidLevel(int.Parse(GameRoom.LevelID), (levelProps, errorProps) =>
                    {

                        if (levelProps == null)
                        {
                            Logs.Error("Error getting level props: " + errorProps);
                        }
                        else
                        {
                            LevelCompletion comp = completion.GetComponent<LevelCompletion>();

                            comp.completeScore = levelProps.levelProps.CompletionScore;
                            comp.completeTime = levelProps.levelProps.TimeLimit;
                            comp.completeTargetScore = levelProps.levelProps.TargetScore;
                            comp.completeMoves = levelProps.levelProps.MoveLimit;
                            comp.condition = levelProps.levelProps.Condition;
                            comp.mechanics = levelProps.levelProps.Mechanics;
                            comp.frozen = levelProps.levelProps.FrozenGrains;
                        }
                        NetworkServer.Spawn(completion);
                    });
                }
                else
                {
                    Msf.Server.Save.GetLevelProperties(int.Parse(GameRoom.LevelID), (levelProps, errorProps) =>
                    {
                        if (levelProps == null)
                        {
                            Logs.Error("Error getting level props: " + errorProps);
                        }
                        else
                        {
                            LevelCompletion comp = completion.GetComponent<LevelCompletion>();

                            comp.completeScore = levelProps.CompletionScore;
                            comp.completeTime = levelProps.TimeLimit;
                            comp.completeTargetScore = levelProps.TargetScore;
                            comp.completeMoves = levelProps.MoveLimit;
                            comp.condition = levelProps.Condition;
                            comp.mechanics = levelProps.Mechanics;
                            comp.frozen = levelProps.FrozenGrains;
                        }
                        NetworkServer.Spawn(completion);
                    });
                }
            }
            
            else
            {
                Msf.Server.Save.GetLevelProperties(int.Parse(singleplayerData.properties["LevelID"]), (levelProps, errorProps) =>
                {
                    if (levelProps == null)
                    {
                        Logs.Error("Error getting level props: " + errorProps);
                    }
                    else
                    {
                        LevelCompletion comp = completion.GetComponent<LevelCompletion>();

                        comp.completeScore = levelProps.CompletionScore;
                        comp.completeTime = levelProps.TimeLimit;
                        comp.completeTargetScore = levelProps.TargetScore;
                        comp.completeMoves = levelProps.MoveLimit;
                        comp.condition = levelProps.Condition;
                        comp.mechanics = levelProps.Mechanics;
                        comp.frozen = levelProps.FrozenGrains;
                    }
                    NetworkServer.Spawn(completion);
                });
            }
            


        }


        StartCoroutine(InstantiateDataNeeds(player));
        
    }

    private IEnumerator InstantiateDataNeeds(UnetMsfPlayer player)
    {
        //Wait until the microstructure has been guarunteed built
        yield return new WaitUntil(() => { return assigner.microstructureBuilt; });
        //Send the orientation data to clients so the connections can be locally built
        var orientations = GameObject.FindGameObjectsWithTag("Orientation");
        foreach(GameObject obj in orientations)
        {
            obj.GetComponent<Orientation>().Initialize(player);
        }
    }
    private IEnumerator InstantiateDataNeeds(NetworkConnection conn)
    {
        //Wait until the microstructure has been guarunteed built
        yield return new WaitUntil(() => { return assigner.microstructureBuilt; });
        //Send the orientation data to clients so the connections can be locally built
        var orientations = GameObject.FindGameObjectsWithTag("Orientation");
        foreach (GameObject obj in orientations)
        {
            obj.GetComponent<Orientation>().Initialize(conn);
        }
    }
    public void PostToLeaderboard(string username, Action callback)
    {
        if (singleplayer && singleplayerData.properties["Mode"] == "Challenge")
        {
            callback.Invoke();
            return;
        }
            
        gameoverDatabasePostCount++;
        if (singleplayer && singleplayerData.properties["Mode"] == "Competitive")
        {
            Msf.Server.Leader.PostCompScore(int.Parse(singleplayerData.properties["LevelID"]), DateTime.UtcNow.ToFileTimeUtc(), username, assigner.currHighScore, (status, message) =>
            {
                if (status != ResponseStatus.Success)
                {
                    Logs.Error("Could not post score to database");
                }

                callback.Invoke();

            });
        }
        else if(gameoverDatabasePostCount < GameRoom.GetPlayers().Count)
        {
            users.Add(username);
            callback.Invoke();
        }
        else
        {
            users.Add(username);
            //Sort the whole team by alphabetical order, so the leaderboard isn't filled with permutations
            string userString = "";
            users.Sort();
            for(int i = 0; i < users.Count - 1; i++)
            {
                userString += users[i] + ", ";
            }

            userString += users[users.Count-1];

            //Post the score to the leaderboard
            if (!singleplayer && SceneManager.GetActiveScene().name == "Raid_Puzzle")
            {
                Msf.Server.Leader.PostLeaderboardScore(int.Parse(GameRoom.LevelID), DateTime.UtcNow.ToFileTimeUtc(), userString, assigner.currHighScore, (status, message) =>
                {
                    if (status != ResponseStatus.Success)
                    {
                        Logs.Error("Could not post score to database");
                    }

                    callback.Invoke();

                });
            }
            else
            {
                callback.Invoke();
            }
            
        }
        

    }
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        if (!singleplayer)
            return;
        if (!spawnedPlayer)
        {
           StartCoroutine(OnServerAddPlayerWait(conn, playerControllerId));
           spawnedPlayer = true;
        }
           
    }


    public IEnumerator OnServerAddPlayerWait(NetworkConnection conn, short playerControllerId)
    {
        while(!singleplayerData.sceneLoading.isDone)
        {
            yield return new WaitForEndOfFrame();
        }
        
        //Set the skybox to the correct background
        SetZoneSkybox(singleplayerData.properties["ZoneID"]);
        //If the grain network has not been created yet, create the grain network
        //Also create the single log file for the time history
        if (GameObject.FindGameObjectWithTag("GameController") == null)
        {
            chatRoomName = "";
            //Spawn the grain network assigner if it's not here
            GameObject networkAssigner = Instantiate(singleton.spawnPrefabs[3]);
            assigner = networkAssigner.GetComponent<GrainNetworkAssigner>();
            var structurePath = singleplayerData.properties["GameStructurePath"];

            Logs.Info("StructurePath in NM : |" + structurePath + "|");

            if (assigner != null && (structurePath != null || structurePath != ""))
            {
#if UNITY_STANDALONE_WIN
                assigner.structure = SaveFunctions.LoadFile(Application.dataPath + @"/Save Files/" + structurePath + ".SAVE");
                string thLocation = Application.dataPath + @"/THData/";
                if (!Directory.Exists(thLocation))
                {
                    Directory.CreateDirectory(thLocation);
                }

                string sessionName = structurePath + " " + singleplayerData.properties[MsfDictKeys.MapName] + " " + System.DateTime.UtcNow.ToString("d-M-yyyy-hh-mm");

                thFilePathName = thLocation + sessionName + ".txt";
                thFileStrippedName = sessionName + ".txt";
                writer = File.CreateText(thLocation + sessionName + ".txt");
#else
                assigner.structure = SaveFunctions.LoadFile(Application.dataPath + @"/Save Files/" + structurePath + ".SAVE");
                string thLocation = Application.dataPath + @"/THData/";
                if (!Directory.Exists(thLocation))
                {
                    Directory.CreateDirectory(thLocation);
                }

                string sessionName = structurePath + " " + singleplayerData.properties[MsfDictKeys.MapName] + " " + System.DateTime.UtcNow.ToString("d-M-yyyy-hh-mm");
                thFilePathName = thLocation + sessionName + ".txt";
                thFileStrippedName = sessionName + ".txt";

                writer = File.CreateText(thLocation + sessionName + ".txt");
#endif

            }
            else
                assigner.structure = null;
            NetworkServer.Spawn(networkAssigner);

        }

        assigner = GameObject.FindGameObjectWithTag("GameController").GetComponent<GrainNetworkAssigner>();

        var playerProfile = GrainPlayerProfileFactory.CreateProfileInServer(Msf.Client.Auth.AccountInfo.Username);
        ObservableDictStringFloat levelScore;
        if (singleplayerData.properties["Mode"] == "Challenge")
            levelScore = playerProfile.GetProperty<ObservableDictStringFloat>(GrainPlayerProfileFactory.highScoresLabChallenge);
        else
            levelScore = playerProfile.GetProperty<ObservableDictStringFloat>(GrainPlayerProfileFactory.highScores);

        Msf.Server.Profiles.FillProfileValues(playerProfile, (success, error) =>
        {
            if (!success)
                Logs.Error("Could not get player profile: " + error);
            //Set up the high score recording for this level
            if (!levelScore.UnderlyingDictionary.ContainsKey(singleplayerData.properties["LevelID"]))
            {
                levelScore.SetValue(singleplayerData.properties["LevelID"], 0);

            }

            // Spawn the player object (https://docs.unity3d.com/Manual/UNetPlayers.html)
            var playerGameObject = Instantiate(playerPrefab);
            spawnedPlayer = true;
            var playerController = playerGameObject.GetComponent<PlayerBehavior>();
            playerController.ChatChannel = chatRoomName;

            playerController.username = Msf.Client.Auth.AccountInfo.Username;
            playerController.levelName = singleplayerData.properties[MsfDictKeys.MapName];
            SetPlayerCubeHighlightMat(playerController, singleplayerData.properties["ZoneID"]);
            NetworkServer.AddPlayerForConnection(conn, playerGameObject, 0);

            assigner.checkHighScore += () =>
            {
                if (levelScore.GetValue(singleplayerData.properties["LevelID"]) < assigner.SGTOutput)
                {
                    levelScore.SetValue(singleplayerData.properties["LevelID"], assigner.SGTOutput);
                }
            };


        });

        //If this is not a tutorial level, create a game completion object
        if (GameObject.FindGameObjectWithTag("GameCompletion") == null)
        {
            GameObject completion = Instantiate(singleton.spawnPrefabs[5]);
            //Fill in the needed level completion values here
            if (singleplayerData.properties["Mode"] == "Competitive")
            {
                Msf.Client.SaveLoad.GetCompLevel(int.Parse(singleplayerData.properties["LevelID"]), (levelProps, errorProps) =>
                {

                    if (levelProps == null)
                    {
                        Logs.Error("Error getting level props: " + errorProps);
                    }
                    else
                    {
                        LevelCompletion comp = completion.GetComponent<LevelCompletion>();

                        comp.completeScore = levelProps.levelProps.CompletionScore;
                        comp.completeTime = levelProps.levelProps.TimeLimit;
                        comp.completeTargetScore = levelProps.levelProps.TargetScore;
                        comp.completeMoves = levelProps.levelProps.MoveLimit;
                        comp.condition = levelProps.levelProps.Condition;
                        comp.mechanics = levelProps.levelProps.Mechanics;
                        comp.frozen = levelProps.levelProps.FrozenGrains;
                    }
                    NetworkServer.Spawn(completion);
                });
            }
            else if (singleplayerData.properties["Mode"] == "Challenge")
            {
                Msf.Client.SaveLoad.GetChalLevel(int.Parse(singleplayerData.properties["LevelID"]), (levelProps, errorProps) =>
                {

                    if (levelProps == null)
                    {
                        Logs.Error("Error getting level props: " + errorProps);
                    }
                    else
                    {
                        LevelCompletion comp = completion.GetComponent<LevelCompletion>();

                        comp.completeScore = levelProps.levelProps.CompletionScore;
                        comp.completeTime = levelProps.levelProps.TimeLimit;
                        comp.completeTargetScore = levelProps.levelProps.TargetScore;
                        comp.completeMoves = levelProps.levelProps.MoveLimit;
                        comp.condition = levelProps.levelProps.Condition;
                        comp.mechanics = levelProps.levelProps.Mechanics;
                        comp.frozen = levelProps.levelProps.FrozenGrains;
                    }
                    NetworkServer.Spawn(completion);
                });
            }
            else
            {
                Msf.Server.Save.GetLevelProperties(int.Parse(singleplayerData.properties["LevelID"]), (levelProps, errorProps) =>
                {
                    if (levelProps == null)
                    {
                        Logs.Error("Error getting level props: " + errorProps);
                    }
                    else
                    {
                        LevelCompletion comp = completion.GetComponent<LevelCompletion>();

                        comp.completeScore = levelProps.CompletionScore;
                        comp.completeTime = levelProps.TimeLimit;
                        comp.completeTargetScore = levelProps.TargetScore;
                        comp.completeMoves = levelProps.MoveLimit;
                        comp.condition = levelProps.Condition;
                        comp.mechanics = levelProps.Mechanics;
                        comp.frozen = levelProps.FrozenGrains;
                        comp.loaded = true;
                    }
                    NetworkServer.Spawn(completion);
                });
            }

        }

        StartCoroutine(InstantiateDataNeeds(conn));
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        if(GameRoom !=null)
            GameRoom.ClientDisconnected(conn);

        if(GameRoom.NetworkManager.numPlayers < 1)
        {
            writer.Close();
            
        }
        base.OnServerDisconnect(conn);
    }
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        
        if (SceneManager.GetActiveScene().name != "Raid_Puzzle")
            SceneManager.LoadSceneAsync("Client");
        else
            SceneManager.LoadSceneAsync("TheLab");
        if (GameRoom != null)
            GameRoom.ClientDisconnected(conn);
    }
    public override void OnStopHost()
    {
#if UNITY_EDITOR
        if (SceneManager.GetActiveScene().name == "Tutorial_Level")
        {
            ProgressLevelProfile();
        }
        base.OnStopHost();
#else
        StreamTimeHistoryFile();
        //Since the tutorial does not have a Completion object, manually progress the player
        if (SceneManager.GetActiveScene().name == "Tutorial_Level")
        {
            ProgressLevelProfile();
        }

        base.OnStopHost();
#endif
    }
    //Should restart the level, data collection, and structure of the scene
    public void RestartLevel()
    {
        //End the previous data collection
        writer.Close();

        //Reset the game structure and data collection file
        GrainNetworkAssigner assigner = GameObject.FindGameObjectWithTag("GameController").GetComponent<GrainNetworkAssigner>();
        
        foreach(Orientation ori in assigner.allGrains)
        {
            string[] oriData = assigner.structure[1].text.Split('\r');

            for(int i = 0; i < oriData.Length; i++)
            {
                string[] orientations = oriData[i].Split(' ', '\t');
                ori.transform.rotation = new Quaternion(float.Parse(orientations[0]), float.Parse(orientations[1]), float.Parse(orientations[2]), float.Parse(orientations[3]));
            }
        }
        string structurePath;
        if (!singleplayer)
            structurePath = GameRoom.GrainStructurePath;
        else
            structurePath = singleplayerData.properties["GameStructurePath"];

        if (assigner != null && (structurePath != null || structurePath != ""))
        {
#if UNITY_STANDALONE_WIN
            assigner.structure = SaveFunctions.LoadFile(Application.dataPath + "\\Save Files\\" + structurePath + ".SAVE");
            string thLocation = Application.dataPath + @"\THData\";
            if (!Directory.Exists(thLocation))
            {
                Directory.CreateDirectory(thLocation);
            }

            string sessionName = structurePath + " " + GameRoom?.MapName + " " + System.DateTime.UtcNow.ToString("d-M-yyyy-hh-mm");

            writer = File.CreateText(thLocation + sessionName + ".txt");
#else
                Debug.Log("Entered the following file: " + Application.dataPath + @"/Save Files/" + structurePath + ".SAVE");
                assigner.structure = SaveFunctions.LoadFile(Application.dataPath + @"/Save Files/" + structurePath + ".SAVE");
                string thLocation = Application.dataPath + @"/THData/";
                if (!Directory.Exists(thLocation))
                {
                    Directory.CreateDirectory(thLocation);
                }

                string sessionName = structurePath + " " + GameRoom.MapName + " " + System.DateTime.UtcNow.ToString("d-M-yyyy-hh-mm");

                writer = File.CreateText(thLocation + sessionName + ".txt");
#endif

        }

        foreach(GameObject ori in GameObject.FindGameObjectsWithTag("Orientation"))
        {
            ori.GetComponent<Orientation>().UpdateDiffusivities();
        }
        //Reset the player values and home grains
        assigner.sgtUpdated = false;

        //Reset the winning conditions and timers
        GameObject.Find("LevelCompletion(Clone)").GetComponent<LevelCompletion>().RestartLevel();

    }

    /// <summary>
    /// Checks upon level completion if the profiles of the players should be advanced to the next stage
    /// </summary>
    public void ProgressLevelProfile()
    {
        if (!singleplayer || singleplayerData.properties["Mode"] == "Competitive" || SceneManager.GetActiveScene().name == "Raid_Puzzle" || singleplayerData.properties["Mode"] == "Challenge")
            return;
        if(singleplayer)
        {
            var user = Msf.Client.Auth.AccountInfo.Username;
            var profile = GrainPlayerProfileFactory.CreateProfileInServer(user);

            Msf.Server.Profiles.FillProfileValues(profile, (success, error) =>
            {
                if (!success)
                {
                    Debug.LogError("Error retrieving player's profile data: " + error);
                }

                var progress = profile.GetProperty<ObservableInt>(GrainPlayerProfileFactory.LevelProgress);
                if (progress.Value == int.Parse(singleplayerData.properties["LevelID"]))
                    progress.Set(progress.Value + 1);
                var player = FindObjectOfType<PlayerBehavior>();
                if(player != null)
                {
                    player.levelProgressed = true;
                }
                
            });
        }
        else
        {
            //Get all of the player profiles
            var players = GameRoom.GetPlayers();

            foreach (string user in players.Keys)
            {
                var profile = GrainPlayerProfileFactory.CreateProfileInServer(user);

                Msf.Server.Profiles.FillProfileValues(profile, (success, error) =>
                {
                    if (!success)
                    {
                        Debug.LogError("Error retrieving player's profile data: " + error);
                    }

                    var progress = profile.GetProperty<ObservableInt>(GrainPlayerProfileFactory.LevelProgress);
                    if (progress.Value == int.Parse(GameRoom.LevelID))
                        progress.Set(progress.Value + 1);

                });
            }
        }
        
    }

    public void StreamTimeHistoryFile()
    {
        try
        {
            writer.Close();
        }
        catch(Exception e)
        {
            Debug.LogError("Failed to stream time file: " + e.ToString());
            return;
        }
        var reader = File.ReadAllBytes(thFilePathName);

        completedFileStream = false;
        using(MemoryStream ms = new MemoryStream())
        {
            using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
            {
                writer.Write(thFileStrippedName);
                writer.Write(reader);
                
            }
            Msf.Client.Connection.Peer.SendMessage(MessageHelper.Create((short) SaveLoadOpCodes.StreamTimeHistory,ms.ToArray()),(status,message)=> 
            {
                if (status==ResponseStatus.Success)
                {
                    completedFileStream = true;
                }
                else
                {
                    Debug.LogError("Could not stream time history file to master server");
                }
            },30, DeliveryMethod.ReliableSequenced);
        }
    }

    public void FinishGame()
    {

    }

    private void SetZoneAssets(string zoneID)
    {
        SetZoneSkybox(zoneID);
        SetZoneBackground(zoneID);
        SetZonePostProcessing(zoneID);
        SetZoneLighting(zoneID);
        SetFogColor(zoneID);
        SetZoneCube(zoneID);
        EnableDisableZoneObjects(zoneID);
    }
    private void SetZoneSkybox(string zoneID)
    {
        
        int parsedID = int.Parse(zoneID)-1;
        if (parsedID < allSkyboxes.Count && parsedID>=0)
            zoneSkybox.material = allSkyboxes[parsedID];
        else
            zoneSkybox.material = allSkyboxes[0];
    }
    private void SetZoneBackground(string zoneID)
    {

        int parsedID = int.Parse(zoneID)-1;
        if(parsedID < allCliffPositions.Count && parsedID >= 0)
        {
            backgroundParentObject.transform.position = allCliffPositions[parsedID];
        }
        else
        {
            backgroundParentObject.transform.position = allCliffPositions[3];
        }
    }
    private void SetZonePostProcessing(string zoneID)
    {
        int parsedID = int.Parse(zoneID) - 1;
        if (parsedID < allPostProc.Count && parsedID >= 0)
        {
            allPostProc[parsedID].gameObject.SetActive(true);
        }
        else
        {
            allPostProc[3].gameObject.SetActive(true);
        }
    }
    private void SetZoneLighting(string zoneID)
    {
        int parsedID = int.Parse(zoneID) - 1;
        if (parsedID < allPostProc.Count && parsedID >= 0)
        {
            individualLevelLighting[parsedID].gameObject.SetActive(true);
        }
        else
        {
            individualLevelLighting[3].gameObject.SetActive(true);
        }
    }
    private void SetZoneCube(string zoneID)
    {
        int parsedID = int.Parse(zoneID) - 1;
        if (parsedID < allPostProc.Count && parsedID >= 0)
        {
            spawnPrefabs[1].GetComponent<Renderer>().material = CubeMaterials[parsedID];
        }

    }
    private void SetFogColor(string zoneID)
    {

        int parsedID = int.Parse(zoneID) - 1;
        if (parsedID < allPostProc.Count && parsedID >= 0)
        {
            RenderSettings.fogColor = levelFogColors[parsedID];
            RenderSettings.fogEndDistance = fogEndDistance[parsedID];
        }
        else
        {
            RenderSettings.fogColor = levelFogColors[3];
            RenderSettings.fogEndDistance = fogEndDistance[3];
        }
    }
    private void EnableDisableZoneObjects(string zoneID)
    {
        int parsedID = int.Parse(zoneID) - 1;
        switch(parsedID)
        {
            //Euphotic zone
            case 0:
                EnableList(EuphoticEnable);
                DisableList(EuphoticDisable);
                break;
            //Dysphotic zone
            case 1:
                EnableList(DysphoticEnable);
                DisableList(DysphoticDisable);
                break;
            //Aphotic zone
            case 2:
                EnableList(AphoticEnable);
                DisableList(AphoticDisable);
                break;
            //Lab zone
            case 3:
                EnableList(LabEnable);
                DisableList(LabDisable);
                break;
            default:
                EnableList(LabEnable);
                DisableList(LabDisable);
                break;
        }
    }
    private void EnableList(List<GameObject> list)
    {
        foreach(GameObject obj in list)
        {
            obj.SetActive(true);
        }
    }
    private void DisableList(List<GameObject> list)
    {
        foreach(GameObject obj in list)
        {
            obj.SetActive(false);
        }
    }
    private void SetPlayerCubeHighlightMat(PlayerBehavior player, string zoneID)
    {
        int parsedID = int.Parse(zoneID) - 1;
        if (parsedID < highlightCubeMat.Count)
        {
            player.highlightCube = highlightCubeMat[parsedID];
        }
        else
        {
            player.highlightCube = highlightCubeMat[0];
        }
        
    }
}
