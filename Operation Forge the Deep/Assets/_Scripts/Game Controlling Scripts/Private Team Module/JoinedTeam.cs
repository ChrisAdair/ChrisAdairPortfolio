using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;
using System;

public class JoinedTeam
{

    public int id;
    public List<string> members;
    public string leader;
    public delegate void TeamUpdate();
    public event TeamUpdate TeamUpdated;

    private IClientSocket _connection;

    public JoinedTeam(IClientSocket connection)
    {
        _connection = connection;
        _connection.SetHandler((short)PrivateTeamOpCodes.TeamChanged, HandleTeamChanged);
        _connection.SetHandler((short)PrivateTeamOpCodes.ClientStart, HandleStartGame);
    }


    #region Handlers

    private void HandleTeamChanged(IIncommingMessage message)
    {
        var packet = message.Deserialize(new TeamPacket());

        id = packet.id;
        members = packet.members;
        leader = packet.leader;
        TeamUpdated.Invoke();
    }

    private void HandleStartGame(IIncommingMessage message)
    {
        var packet = message.Deserialize(new StartGamePacket());
        Msf.Client.Rooms.GetAccess(packet.roomId, (access, error) =>
        {
            if (access == null)
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                        DialogBoxData.CreateInfo("Failed to get access to room: " + error));

                Logs.Error("Failed to get access to room: " + error);

                return;
            }
            TriggerBedChange.singleton.LevelTransition(packet.bedSong);
        });
    }
    #endregion
}
