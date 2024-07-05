using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;


public class RaidLobbyFactorySetup : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var module = FindObjectOfType<LobbiesModule>();

        module.AddFactory(new LobbyFactoryAnonymous("RaidLobby", module, RaidLobbyFactory.GroupRaid));
    }

}
