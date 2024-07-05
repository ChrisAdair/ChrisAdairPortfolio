using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;
using System;

public class PrivateTeamClient : MsfBaseClient
{
    public event Action Invited;
    public InvitePacket invitation;
    public JoinedTeam team;

    public event Action<List<bool>> readyCheck;
    public event Action<List<bool>> readyStart;

    public int songId;

    private IClientSocket connection;
    private SpawnRequestController _spawnRequest;

    public PrivateTeamClient(IClientSocket connection) : base(connection)
    {
        this.connection = connection;

        connection.SetHandler((short)PrivateTeamOpCodes.Invited, HandleInvitation);
        connection.SetHandler((short)PrivateTeamOpCodes.ServerReadyStart, HandleReadyCheckStart);
        connection.SetHandler((short)PrivateTeamOpCodes.ServerReadyUpdate, HandleReadyCheckUpdate);
    }


    public void MakeTeam(Action callback)
    {
        connection.SendMessage(MessageHelper.Create((short)PrivateTeamOpCodes.MakeTeam), (response, message) =>
        {
            var packet = message.Deserialize(new TeamPacket());

            team = new JoinedTeam(connection)
            {
                id = packet.id,
                leader = packet.leader,
                members = packet.members
            };
            callback.Invoke();
        });
    }

    public void JoinTeam(int teamId, Action<JoinedTeam> callback)
    {
        connection.SendMessage(MessageHelper.Create((short)PrivateTeamOpCodes.JoinTeam, teamId), (response, message) =>
        {
            if(response != ResponseStatus.Success)
            {
                Logs.Error("Could not connect to team");
            }
            else
            {
                var packet = message.Deserialize(new TeamPacket());
                team = new JoinedTeam(connection)
                {
                    id = packet.id,
                    leader = packet.leader,
                    members = packet.members
                };
                callback.Invoke(team);
            }
        });
    }

    public void SendInvitation(string username, Action callback)
    {
        connection.SendMessage(MessageHelper.Create((short)PrivateTeamOpCodes.InvitePlayer, username), (response, message) =>
         {
             if(response != ResponseStatus.Success)
             {
                 Debug.Log("Player is not logged in.");
             }
         });
    }

    public void CheckTeam(Action<bool> callback)
    {
        connection.SendMessage(MessageHelper.Create((short)PrivateTeamOpCodes.CheckTeam), (response, message) =>
         {

             if(response == ResponseStatus.Success)
             {
                 var packet = message.Deserialize(new TeamPacket());

                 team = new JoinedTeam(connection)
                 {
                     id = packet.id,
                     leader = packet.leader,
                     members = packet.members
                 };
                 callback.Invoke(true);
             }
             else
             {
                 callback.Invoke(false);
             }
         });
    }
    public void SetLeader(string username, Action callback)
    {
        if (team == null)
            return;

        connection.SendMessage(MessageHelper.Create((short)PrivateTeamOpCodes.MakeLeader, username));
    }
    public void StartGame(SpawnRequestController spawnRequest, int songId)
    {
        _spawnRequest = spawnRequest;
        spawnRequest.StatusChanged += OnRequestStatusChange;
        this.songId = songId;
    }
    public void StartGame(int roomID, Action callback)
    {
        var packet = new StartGamePacket()
        {
            roomId = roomID,
            bedSong = songId
        };
        connection.SendMessage(MessageHelper.Create((short)PrivateTeamOpCodes.StartGame, packet));
        callback.Invoke();
    }
    private void OnRequestStatusChange(SpawnStatus status)
    {
        if (status == SpawnStatus.Finalized)
        {
            _spawnRequest.GetFinalizationData((data, error) =>
            {
                if (!data.ContainsKey(MsfDictKeys.RoomId))
                {
                    throw new System.Exception("Game server finalized, but didn't include room id");
                }

                var roomId = int.Parse(data[MsfDictKeys.RoomId]);

                StartGame(roomId, () => { });
            });
        }
    }
    public void SendReadyCheck()
    {
        connection.SendMessage(MessageHelper.Create((short)PrivateTeamOpCodes.ClientReadySend));
    }
    public void LeaveTeam(Action callback)
    {
        connection.SendMessage(MessageHelper.Create((short)PrivateTeamOpCodes.LeaveTeam));
        callback.Invoke();
    }
    #region Handlers

    private void HandleInvitation(IIncommingMessage message)
    {
        invitation = message.Deserialize(new InvitePacket());

        Invited.Invoke();
    }

    private void HandleReadyCheckStart(IIncommingMessage message)
    {
        var packet = message.Deserialize(new ReadyUpdatePacket());
        readyStart.Invoke(packet.readyList);
    }

    private void HandleReadyCheckUpdate(IIncommingMessage message)
    {
        var packet = message.Deserialize(new ReadyUpdatePacket());
        readyCheck.Invoke(packet.readyList);
    }
    #endregion
}
