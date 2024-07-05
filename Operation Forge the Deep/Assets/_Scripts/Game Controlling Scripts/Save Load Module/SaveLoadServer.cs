using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;
using System;

public class SaveLoadServer : MsfBaseClient {

    public delegate void LoadLevelPropsCallback(LevelPropsPacket props, string error);

    public SaveLoadServer(IClientSocket connection) : base(connection)
    {
    }

    public void GetLevelProperties(int levelID, LoadLevelPropsCallback callback)
    {
        GetLevelProperties(levelID, callback, Connection);
    }

    public void GetLevelProperties(int levelID, LoadLevelPropsCallback callback, IClientSocket connection)
    {
        if (!connection.IsConnected)
        {
            callback.Invoke(null, "Not connected to the server!");
        }
        connection.SendMessage((short)SaveLoadOpCodes.GetLevelProps, levelID, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                callback.Invoke(null, "Could not retrieve level properties: " + status);
            }
            else
            {
                LevelPropsPacket props = response.Deserialize(new LevelPropsPacket());
                callback.Invoke(props, null);
            }
        });
    }

    public void GetRaidLevel(int levelID, Action<RaidDataPacket, string> callback)
    {
        GetRaidLevel(levelID, callback, Connection);
    }

    public void GetRaidLevel(int levelID, Action<RaidDataPacket, string> callback, IClientSocket connection)
    {
        if (!connection.IsConnected)
        {
            callback.Invoke(null, "Not connected to the server!");
        }
        connection.SendMessage((short)SaveLoadOpCodes.GetRaidLevel, levelID, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                callback.Invoke(null, "Could not retrieve level properties: " + status);
            }
            else
            {
                RaidDataPacket props = response.Deserialize(new RaidDataPacket());
                callback.Invoke(props, null);
            }
        });
    }
}
