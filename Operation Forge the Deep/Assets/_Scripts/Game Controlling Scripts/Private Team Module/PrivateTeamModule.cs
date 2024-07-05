using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;
using System.Linq;
using System.Text;

public enum PrivateTeamOpCodes
{
    MakeTeam = 80,
    MakeLeader = 81,
    InvitePlayer = 82,
    JoinTeam = 83,
    LeaveTeam = 84,
    KickPlayer = 85,
    StartGame = 86,
    Invited = 87,
    TeamChanged = 88,
    ClientStart = 89,
    CheckTeam = 90,
    ClientReadySend = 91,
    ServerReadyStart = 92,
    ServerReadyUpdate = 93
}
public class PrivateTeamModule : ServerModuleBehaviour
{

    private Dictionary<int, ITeam> _teams;

    private void Awake()
    {
        AddOptionalDependency<AuthModule>();
    }

    public override void Initialize(IServer server)
    {
        base.Initialize(server);

        _teams = new Dictionary<int, ITeam>();

        var auth = server.GetModule<AuthModule>();
        auth.LoggedIn += GenerateTeamExtention;
        auth.LoggedOut += CheckLogoutTeam;

        server.SetHandler((short)PrivateTeamOpCodes.MakeTeam, HandleMakeTeam);
        server.SetHandler((short)PrivateTeamOpCodes.MakeLeader, HandleMakeLeader);
        server.SetHandler((short)PrivateTeamOpCodes.InvitePlayer, HandleInvitePlayer);
        server.SetHandler((short)PrivateTeamOpCodes.JoinTeam, HandleJoinTeam);
        server.SetHandler((short)PrivateTeamOpCodes.LeaveTeam, HandleLeaveTeam);
        server.SetHandler((short)PrivateTeamOpCodes.KickPlayer, HandleKickPlayer);
        server.SetHandler((short)PrivateTeamOpCodes.StartGame, HandleStartGame);
        server.SetHandler((short)PrivateTeamOpCodes.CheckTeam, HandleCheckTeam);
        server.SetHandler((short)PrivateTeamOpCodes.ClientReadySend, HandleClientReadySend);
    }


    private bool CheckLeader(IPeer player)
    {
        var teamExtension = player.GetExtension<ITeamExtension>();
        return _teams[teamExtension.TeamId].Leader == teamExtension;

    }
    private int GenerateTeamId()
    {
        if(_teams.Count == 0)
        {
            return 0;
        }
        for(int i = 0; i <= _teams.Keys.Max(); i++)
        {
            if (_teams.ContainsKey(i))
                continue;
            return i;
        }
        return _teams.Keys.Max()+1;
    }

    public void GenerateTeamExtention(IUserExtension user)
    {
        user.Peer.AddExtension<ITeamExtension>(new TeamExtension(user.Peer));

    }

    private void CheckLogoutTeam(IUserExtension user)
    {
        int teamId = user.Peer.GetExtension<ITeamExtension>().TeamId;

        ITeam team;
        if(_teams.TryGetValue(teamId, out team))
        {
            team.OnLeaveTeam(user.Peer.GetExtension<ITeamExtension>());

            if (team.Members.Count <= 0)
            {
                _teams.Remove(teamId);
            }
        }
        
    }
    #region Handlers

    private void HandleInvitePlayer(IIncommingMessage message)
    {
        var auth = Server.GetModule<AuthModule>();

        string invite = message.AsString();
        invite = invite.Trim(new char[] { '\u200b', ' ' });
        if (auth.IsUserLoggedIn(invite))
        {
            var package = message.Peer.GetExtension<ITeamExtension>().CreateInvitePacket();

            auth.GetLoggedInUser(invite).Peer.SendMessage(MessageHelper.Create((short)PrivateTeamOpCodes.Invited, package));
            message.Respond(ResponseStatus.Success);
        }
        else
        {
            message.Respond(ResponseStatus.Invalid);
        }
    }
    private void HandleMakeTeam(IIncommingMessage message)
    {
        var playerExtention = message.Peer.GetExtension<ITeamExtension>();
        
        PrivateTeam team = new PrivateTeam(GenerateTeamId(), playerExtention);

        _teams.Add(team.ID, team);
        playerExtention.Team = team;
        playerExtention.TeamId = team.ID;
        var packet = team.CreateTeamPacket();

        message.Respond(packet, ResponseStatus.Success);
    }

    private void HandleCheckTeam(IIncommingMessage message)
    {
        var playerExtention = message.Peer.GetExtension<ITeamExtension>();

        if(playerExtention != null)
        {
            ITeam team;
            bool gotTeam = _teams.TryGetValue(playerExtention.TeamId, out team);

            if (gotTeam)
            {
                message.Respond(team.CreateTeamPacket(), ResponseStatus.Success);
            }
            else
            {
                message.Respond(ResponseStatus.Invalid);
            }
        }
        else
        {
            message.Respond(ResponseStatus.Error);
        }
    }
    private void HandleKickPlayer(IIncommingMessage message)
    {
        var auth = Server.GetModule<AuthModule>();

        string kick = message.AsString();
        kick = kick.Trim(new char[] { '\u200b', ' ' });
        if (CheckLeader(message.Peer))
        {
            int teamId = message.Peer.GetExtension<ITeamExtension>().TeamId;
            _teams[teamId].OnLeaveTeam(auth.GetLoggedInUser(kick).Peer.GetExtension<ITeamExtension>());
        }
    }
    private void HandleStartGame(IIncommingMessage message)
    {
        var packet = message.Deserialize(new StartGamePacket());
        var player = message.Peer.GetExtension<ITeamExtension>();
        if (CheckLeader(player.Peer))
        {
            ITeam team;
            _teams.TryGetValue(player.TeamId, out team);
            //Change this to start a ready check
            team.SendReadyCheck();
            team.SetGameSong(packet.bedSong);
            //team.OnStartGame(message.AsInt(), player);
        }
        
        
    }
    private void HandleMakeLeader(IIncommingMessage message)
    {
        var teamId = message.Peer.GetExtension<ITeamExtension>().TeamId;
        string invite = message.AsString();
        invite = invite.Trim(new char[] { '\u200b', ' ' });
        var auth = Server.GetModule<AuthModule>();
        _teams[teamId].SetLeader(auth.GetLoggedInUser(invite).Peer.GetExtension<ITeamExtension>(), message.Peer.GetExtension<ITeamExtension>());
    }
    private void HandleJoinTeam(IIncommingMessage message)
    {
        var teamExtention = message.Peer.GetExtension<ITeamExtension>();
        if(teamExtention == null)
        {
            teamExtention = message.Peer.AddExtension<ITeamExtension>(new TeamExtension(message.Peer));
        }
        teamExtention.TeamId = message.AsInt();
        if (!_teams.ContainsKey(teamExtention.TeamId))
        {
            message.Respond(ResponseStatus.Invalid);
        }
        else
        {
            teamExtention.Team = _teams[teamExtention.TeamId];
            _teams[teamExtention.TeamId].OnJoinTeam(teamExtention);

            var packet = _teams[teamExtention.TeamId].CreateTeamPacket();

            message.Respond(packet, ResponseStatus.Success);
        }
        
    }
    private void HandleLeaveTeam(IIncommingMessage message)
    {
        int teamId = message.Peer.GetExtension<ITeamExtension>().TeamId;

        _teams[teamId]?.OnLeaveTeam(message.Peer.GetExtension<ITeamExtension>());

        if(_teams[teamId].Members.Count <= 0)
        {
            _teams.Remove(teamId);
        }
    }

    private void HandleClientReadySend(IIncommingMessage message)
    {
        var playerExt = message.Peer.GetExtension<ITeamExtension>();

        if(playerExt.Team != null)
        {
            _teams[playerExt.TeamId].RecieveReadyCheck(playerExt);
        }
    }
    #endregion
}
