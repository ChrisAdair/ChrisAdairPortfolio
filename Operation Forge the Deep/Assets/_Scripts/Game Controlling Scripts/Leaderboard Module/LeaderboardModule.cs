using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;


public enum LeaderboardOpCodes
{
    GetDailyScores = 40,
    GetMonthlyScores = 41,
    PostScore = 42,
    GetDailyCompScores = 43,
    GetMonthlyCompScores = 44,
    PostCompScore = 45
}
public enum LeaderboardType
{
    Raid,
    Competitive
}
public class LeaderboardModule : ServerModuleBehaviour
{

    AuthModule auth;
    private void Awake()
    {
        AddOptionalDependency<AuthModule>();

    }
    public override void Initialize(IServer server)
    {
        base.Initialize(server);

        server.SetHandler((short)LeaderboardOpCodes.GetDailyScores, HandleGetDailyScores);
        server.SetHandler((short)LeaderboardOpCodes.GetMonthlyScores, HandleGetMonthlyScores);
        server.SetHandler((short)LeaderboardOpCodes.GetDailyCompScores, HandleGetDailyComp);
        server.SetHandler((short)LeaderboardOpCodes.GetMonthlyCompScores, HandleGetMonthlyComp);
        server.SetHandler((short)LeaderboardOpCodes.PostScore, HandlePostScore);
        server.SetHandler((short)LeaderboardOpCodes.PostCompScore, HandlePostCompScore);

        auth = server.GetModule<AuthModule>();
    }


    #region Handlers

    private void HandleGetDailyScores(IIncommingMessage message)
    {
        var db = Msf.Server.DbAccessors.GetAccessor<ILeaderboardDatabase>();

        db.GetDailyLeaderboard(message.AsInt(), (leaderboard) =>
        {
            var packet = new LeaderboardPacket()
            {
                leadType = leaderboard.LeadType,
                userValues = leaderboard.UserValues
            };

            message.Respond(packet,ResponseStatus.Success);
        });
    }
    private void HandleGetMonthlyScores(IIncommingMessage message)
    {
        var db = Msf.Server.DbAccessors.GetAccessor<ILeaderboardDatabase>();

        db.GetMonthlyLeaderboard(message.AsInt(), (leaderboard) =>
        {
            var packet = new LeaderboardPacket()
            {
                leadType = leaderboard.LeadType,
                userValues = leaderboard.UserValues
            };

            message.Respond(packet,ResponseStatus.Success);
        });
    }
    private void HandlePostScore(IIncommingMessage message)
    {
        var db = Msf.Server.DbAccessors.GetAccessor<ILeaderboardDatabase>();

        var postInfo = message.Deserialize(new PostScorePacket());
        db.PostScore(postInfo, (success) =>
        {
            if (!success)
            {
                Logs.Error("Failed to post the score to the database");
                message.Respond(ResponseStatus.Failed);
            }
            else
            {
                message.Respond(ResponseStatus.Success);
            }
        });
    }

    private void HandleGetDailyComp(IIncommingMessage message)
    {
        var db = Msf.Server.DbAccessors.GetAccessor<CompetitiveLeaderboardDbMysql>();

        db.GetDailyLeaderboard(message.AsInt(), (leaderboard) =>
        {
            var packet = new LeaderboardPacket()
            {
                leadType = leaderboard.LeadType,
                userValues = leaderboard.UserValues
            };

            message.Respond(packet, ResponseStatus.Success);
        });
    }
    private void HandleGetMonthlyComp(IIncommingMessage message)
    {
        var db = Msf.Server.DbAccessors.GetAccessor<CompetitiveLeaderboardDbMysql>();

        db.GetMonthlyLeaderboard(message.AsInt(), (leaderboard) =>
        {
            var packet = new LeaderboardPacket()
            {
                leadType = leaderboard.LeadType,
                userValues = leaderboard.UserValues
            };

            message.Respond(packet, ResponseStatus.Success);
        });
    }
    private void HandlePostCompScore(IIncommingMessage message)
    {
        var db = Msf.Server.DbAccessors.GetAccessor<CompetitiveLeaderboardDbMysql>();

        var postInfo = message.Deserialize(new PostScorePacket());
        db.PostScore(postInfo, (success) =>
        {
            if (!success)
            {
                Logs.Error("Failed to post the score to the database");
                message.Respond(ResponseStatus.Failed);
            }
            else
            {
                message.Respond(ResponseStatus.Success);
            }
        });
    }
    #endregion
}
