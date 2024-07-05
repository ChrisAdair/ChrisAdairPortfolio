using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;
using System;

public class LeaderboardServer : MsfBaseClient
{
    public LeaderboardServer(IClientSocket connection) : base(connection)
    {
    }

    public void PostLeaderboardScore(int levelID, long gameTime, string users, float score, ResponseCallback callback)
    {
        var packet = new PostScorePacket()
        {
            levelID = levelID,
            fileTime = gameTime,
            username = users,
            score = score
        };

        Connection.SendMessage(MessageHelper.Create((short)LeaderboardOpCodes.PostScore, packet),(status,message)=> 
        {
            if(status != ResponseStatus.Success)
            {
                callback.Invoke(status, message);
                Logs.Error("Could not post score to the database.");
            }
            else
            {
                callback.Invoke(ResponseStatus.Success, null);
            }
            
        });

    }

    public void PostCompScore(int levelID, long gameTime, string users, float score, ResponseCallback callback)
    {
        var packet = new PostScorePacket()
        {
            levelID = levelID,
            fileTime = gameTime,
            username = users,
            score = score
        };

        Connection.SendMessage(MessageHelper.Create((short)LeaderboardOpCodes.PostCompScore, packet), (status, message) =>
        {
            if (status != ResponseStatus.Success)
            {
                callback.Invoke(status, message);
                Logs.Error("Could not post score to the database.");
            }
            else
            {
                callback.Invoke(ResponseStatus.Success, null);
            }

        });

    }
}
