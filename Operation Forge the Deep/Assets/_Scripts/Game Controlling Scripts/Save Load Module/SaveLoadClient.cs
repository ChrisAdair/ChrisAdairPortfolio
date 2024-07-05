using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;

public class SaveLoadClient : MsfBaseClient
{

    public delegate void LoadLevelCallback(LoadLevelPacket level, string error);
    public delegate void LoadLevelPropsCallback(LevelPropsPacket props, string error);

    public LoadLevelPacket level;
    public bool complete;

    public SaveLoadClient(IClientSocket connection) : base(connection)
    {
    }



    public void GetLevelData(int levelID, LoadLevelCallback callback)
    {
        GetLevelData(levelID, callback, Connection);
    }
    public void GetLevelData(int levelID, LoadLevelCallback callback, IClientSocket connection)
    {
        if (!connection.IsConnected)
        {
            callback.Invoke(null, "Not connected");
            return;
        }

        connection.SendMessage((short)SaveLoadOpCodes.GetLevels, levelID, (status, response) =>
        {
            if(status != ResponseStatus.Success)
            {
                callback.Invoke(null, "Level could not be loaded");
                return;
            }
            
            level = response.Deserialize(new LoadLevelPacket());
            callback.Invoke(level, null);
        });

        
    }
    public void GetNumberOfLevels(Action<int> callback)
    {
        GetNumberOfLevels(callback, Connection);
    }
    public void GetNumberOfLevels(Action<int> callback, IClientSocket connection)
    {
        if (!connection.IsConnected)
        {
            callback.Invoke(-2);
        }

        int numLevels = 0;
        connection.SendMessage((short)SaveLoadOpCodes.GetNumberOfLevels, (status, response) =>
        {
            if(status != ResponseStatus.Success)
            {
                callback.Invoke(-1);
            }
            numLevels = int.Parse(response.AsString());
            callback.Invoke(numLevels);
        });

        
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
            if(status != ResponseStatus.Success)
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

    public void GetNumberOfCompLevels(Action<int> callback)
    {
        GetNumberOfCompLevels(callback, Connection);
    }
    public void GetNumberOfCompLevels(Action<int> callback, IClientSocket connection)
    {
        if (!connection.IsConnected)
        {
            callback.Invoke(-2);
        }

        int numLevels = 0;
        connection.SendMessage((short)SaveLoadOpCodes.GetNumberOfCompLevels, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                callback.Invoke(-1);
            }
            numLevels = response.AsInt();
            callback.Invoke(numLevels);
        });
    }
    public void GetCompLevel(int levelID, Action<CompetitiveDataPacket, string> callback)
    {
        GetCompLevel(levelID, callback, Connection);
    }
    public void GetCompLevel(int levelID, Action<CompetitiveDataPacket, string> callback, IClientSocket connection)
    {
        if (!connection.IsConnected)
        {
            callback.Invoke(null, "Not connected to the server!");
        }
        connection.SendMessage((short)SaveLoadOpCodes.GetCompetitiveLevel, levelID, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                callback.Invoke(null, "Could not retrieve level properties: " + status);
            }
            else
            {
                CompetitiveDataPacket props = response.Deserialize(new CompetitiveDataPacket());
                callback.Invoke(props, null);
            }
        });
    }


    public void GetNumberOfChalLevels(Action<int> callback)
    {
        GetNumberOfChalLevels(callback, Connection);
    }
    public void GetNumberOfChalLevels(Action<int> callback, IClientSocket connection)
    {
        if (!connection.IsConnected)
        {
            callback.Invoke(-2);
        }

        int numLevels = 0;
        connection.SendMessage((short)SaveLoadOpCodes.GetNumberOfChalLevels, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                callback.Invoke(-1);
            }
            numLevels = response.AsInt();
            callback.Invoke(numLevels);
        });
    }
    public void GetChalLevel(int levelID, Action<CompetitiveDataPacket, string> callback)
    {
        GetChalLevel(levelID, callback, Connection);
    }
    public void GetChalLevel(int levelID, Action<CompetitiveDataPacket, string> callback, IClientSocket connection)
    {
        if (!connection.IsConnected)
        {
            callback.Invoke(null, "Not connected to the server!");
        }
        connection.SendMessage((short)SaveLoadOpCodes.GetChallengeLevel, levelID, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                callback.Invoke(null, "Could not retrieve level properties: " + status);
            }
            else
            {
                CompetitiveDataPacket props = response.Deserialize(new CompetitiveDataPacket());
                callback.Invoke(props, null);
            }
        });
    }
    public void GetLoadingHint(Action<string,string> callback)
    {
        if(!Connection.IsConnected)
        {
            callback.Invoke(null, "Not connected to the server!");
        }
        complete = false;
        Connection.SendMessage((short)SaveLoadOpCodes.GetLoadingHint, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                callback.Invoke(null, "Could not retrieve loading hint: " + status);
            }
            else
            {
                string hint = response.AsString();
                callback.Invoke(hint, null);
            }
            complete = true;
        });
    }
}
