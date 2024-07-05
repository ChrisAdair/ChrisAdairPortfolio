using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;

public class RaidLobbyFactory
{

    /// <summary>
    /// Handles the creation of an automatic raid lobby for a minimum of 4 players
    /// </summary>
    /// <param name="module"></param>
    /// <param name="properties"> Requires the MsfDictKeys.MaxPlayers used</param>
    /// <param name="creator"></param>
    /// <returns></returns>
    public static ILobby GroupRaid(LobbiesModule module, Dictionary<string, string> properties, IPeer creator)
    {
        //Create default team for entire group
        var team = new LobbyTeam("")
        {
            MaxPlayers = int.Parse(properties[MsfDictKeys.MaxPlayers]),
            MinPlayers = 2
        };

        var config = new LobbyConfig()
        {
            EnableReadySystem = false,
            EnableManualStart = false,
            AllowJoiningWhenGameIsLive = false
        };

        var lobby = new BaseLobbyAuto(module.GenerateLobbyId(), new[] { team }, module, config)
        {
                Name = "DefaultLobby"
        };

        lobby.SetLobbyProperties(properties);

        lobby.StartAutomation();

        return lobby;
    }
}
