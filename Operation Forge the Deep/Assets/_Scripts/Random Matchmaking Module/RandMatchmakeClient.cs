using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;

public delegate void FindGameCallback(GameInfoPacket game);

public class RandMatchmakeClient : MsfMatchmakerClient {


    public RandMatchmakeClient(IClientSocket connection) : base(connection)
    {

    }

    public void RandomMatchmake(FindGameCallback callback, IClientSocket connection)
    {
        if (!connection.IsConnected)
        {
            Logs.Error("Not connected");
            callback.Invoke(null);
            return;
        }

        connection.SendMessage((short)RandMatchmakeOpCodes.queueUp, (status, response) =>
         {
             if(status != ResponseStatus.Success)
             {
                 Logs.Error("Failed to find game");
                 callback.Invoke(null);
                 return;
             }

             GameInfoPacket game = response.Deserialize(new GameInfoPacket());
             if (game != null)
             {
                 callback.Invoke(game);

             }
                
         });
    }

    public void CancelMatchmake(Action<bool> callback, IClientSocket connection)
    {
        bool success = false;
        if (!connection.IsConnected)
        {
            Logs.Error("Not connected");
            callback.Invoke(success);
            return;
        }

        connection.SendMessage((short)RandMatchmakeOpCodes.cancelQueue, (status, response) =>
        {
            if(status != ResponseStatus.Success)
            {
                Logs.Error("Error: " + response.AsString());
                success = false;
                callback.Invoke(success);
            }
            success = true;
            callback.Invoke(success);
        });
    }

}
