//#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using UnityEngine;
//#endif

namespace Barebones.MasterServer
{
    public class MySqlDbFactory : MonoBehaviour
    {
        public ServerBehaviour Server;

        public string DefaultConnectionString = "server=ipAddress;uid=user;pwd=pass;database=data";

        protected virtual void Awake()
        {
            Server = Server ?? GetComponentInParent<ServerBehaviour>();

            if (Server == null)
            {
                Logs.Error("Database Factory server is not set. Make sure db factory " +
                           "is a child of ServerBehaviour, or that the Server property is set");
                return;
            }

            Server.Started += OnServerStarted;

            // If server is already running
            if (Server.IsRunning)
                OnServerStarted();
        }

        protected virtual void OnServerStarted()
        {
//#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR
            try
            {
                var connectionString = Msf.Args.IsProvided(Msf.Args.Names.DbConnectionString)
                    ? Msf.Args.DbConnectionString
                    : DefaultConnectionString;

                Msf.Server.DbAccessors.SetAccessor<IAuthDatabase>(new AuthDbMysql(connectionString));
                Msf.Server.DbAccessors.SetAccessor<IProfilesDatabase>(new ProfilesDbMysql(connectionString));
                Msf.Server.DbAccessors.SetAccessor<ISaveDatabase>(new SaveLoadDbMysql(connectionString));
                Msf.Server.DbAccessors.SetAccessor<ILeaderboardDatabase>(new RaidLeaderboardDbMysql(connectionString));
                Msf.Server.DbAccessors.SetAccessor<CompetitiveLeaderboardDbMysql>(new CompetitiveLeaderboardDbMysql(connectionString));

            }
            catch
            {
                Logs.Error("Failed to connect to database");
                throw;
            }
//#endif
        }

        protected virtual void OnDestroy()
        {
            if (Server != null)
                Server.Started -= OnServerStarted;
        }
    }
}

