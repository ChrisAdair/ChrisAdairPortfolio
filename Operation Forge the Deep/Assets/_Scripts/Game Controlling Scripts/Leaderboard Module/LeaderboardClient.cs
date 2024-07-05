using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Barebones.MasterServer;
using Barebones.Networking;

public class LeaderboardClient : MsfBaseClient
{


    public LeaderboardClient(IClientSocket connection) : base(connection)
    {
    }

    public void GetDailyLeaderboardData(int levelID, Action<LeaderboardPacket> callback)
    {
        GetDailyLeaderboardData(levelID, callback, Connection);
        
    }

    public void GetDailyLeaderboardData(int levelID, Action<LeaderboardPacket> callback, IClientSocket connection)
    {

        connection.SendMessage(MessageHelper.Create((short)LeaderboardOpCodes.GetDailyScores, levelID), (response, message) =>
        {
            if(response != ResponseStatus.Success)
            {
                Logs.Error("Could not retrieve daily leaderboard data.");
            }
            else
            {
                callback.Invoke(message.Deserialize(new LeaderboardPacket()));
            }

        });
    }

    public void GetMonthlyLeaderboardData(int levelID, Action<LeaderboardPacket> callback)
    {
        GetMonthlyLeaderboardData(levelID, callback, Connection);

    }

    public void GetMonthlyLeaderboardData(int levelID, Action<LeaderboardPacket> callback, IClientSocket connection)
    {

        connection.SendMessage(MessageHelper.Create((short)LeaderboardOpCodes.GetMonthlyScores, levelID), (response, message) =>
        {
            if (response != ResponseStatus.Success)
            {
                Logs.Error("Could not retrieve monthly leaderboard data.");
            }
            else
            {
                callback.Invoke(message.Deserialize(new LeaderboardPacket()));
            }

        });
    }


    public void GetDailyCompData(int levelID, Action<LeaderboardPacket> callback)
    {
        GetDailyCompData(levelID, callback, Connection);

    }

    public void GetDailyCompData(int levelID, Action<LeaderboardPacket> callback, IClientSocket connection)
    {

        connection.SendMessage(MessageHelper.Create((short)LeaderboardOpCodes.GetDailyCompScores, levelID), (response, message) =>
        {
            if (response != ResponseStatus.Success)
            {
                Logs.Error("Could not retrieve daily leaderboard data.");
            }
            else
            {
                callback.Invoke(message.Deserialize(new LeaderboardPacket()));
            }

        });
    }

    public void GetMonthlyCompData(int levelID, Action<LeaderboardPacket> callback)
    {
        GetMonthlyCompData(levelID, callback, Connection);

    }

    public void GetMonthlyCompData(int levelID, Action<LeaderboardPacket> callback, IClientSocket connection)
    {

        connection.SendMessage(MessageHelper.Create((short)LeaderboardOpCodes.GetMonthlyCompScores, levelID), (response, message) =>
        {
            if (response != ResponseStatus.Success)
            {
                Logs.Error("Could not retrieve monthly leaderboard data.");
            }
            else
            {
                callback.Invoke(message.Deserialize(new LeaderboardPacket()));
            }

        });
    }
}
