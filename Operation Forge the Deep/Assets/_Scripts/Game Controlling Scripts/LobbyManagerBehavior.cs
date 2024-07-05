using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LobbyManagerBehavior : NetworkLobbyManager {

    public static string currentStructureToLoad = "";
    public static TextAsset[] structure;

    public override void OnLobbyServerPlayersReady()
    {
        if(currentStructureToLoad != "New Game")
            structure = SaveFunctions.LoadFile(currentStructureToLoad);


        base.OnLobbyServerPlayersReady();
    }
    public void Start()
    {
        StartHost();
    }
}
