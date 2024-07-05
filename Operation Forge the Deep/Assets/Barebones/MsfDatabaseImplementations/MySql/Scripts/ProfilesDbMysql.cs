using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class ProfilesDbMysql : IProfilesDatabase
    {
        private string _connectionString;

        public ProfilesDbMysql(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void RestoreProfile(ObservableServerProfile profile, Action doneCallback)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                try
                {
                    con.Open();
                }
                catch(MySqlException ex)
                {
                    switch (ex.Number)
                    {
                        case 0:
                            Debug.LogError("Cannot connect to server.  Contact administrator");
                            break;
                        case 1045:
                            Debug.LogError("Invalid username/password, please try again");
                            break;
                    }
                    throw;
                }

                cmd.Connection = con;
                cmd.CommandText = "SELECT profile_values.* " +
                                  "FROM profile_values " +
                                  "INNER JOIN accounts " +
                                  "ON accounts.account_id = profile_values.account_id " +
                                  "WHERE username = @username;";
                cmd.Parameters.AddWithValue("@username", profile.Username);

                var reader = cmd.ExecuteReader();

                // There's no such data
                if (!reader.HasRows)
                {
                    doneCallback.Invoke();
                    return;
                }
                    

                var data = new Dictionary<short, string>();

                while (reader.Read())
                {
                    var key = reader.GetInt16("value_key");
                    var value = reader["value_value"] as string ?? "";
                    data.Add(key, value);
                }

                profile.FromStrings(data);
            }

            doneCallback.Invoke();
        }

        public void UpdateProfile(ObservableServerProfile profile, Action doneCallback)
        {
            if (!profile.ShouldBeSavedToDatabase)
                return;

            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT account_id FROM accounts " +
                                  "WHERE username = @username";
                cmd.Parameters.AddWithValue("@username", profile.Username);

                var reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    Logs.Error("Tried to save a profile of a user who has no account: " + profile.Username);
                    return;
                }

                reader.Read();

                var accountId = reader.GetInt32("account_id");

                reader.Close();
                cmd.Parameters.Clear();

                cmd.Connection = con;

                foreach (var unsavedProp in profile.UnsavedProperties)
                {
                    cmd.CommandText = "INSERT INTO profile_values (account_id, value_key, value_value) " +
                                  "VALUES (@account_id, @value_key, @value_value)" +
                                  "ON DUPLICATE KEY UPDATE value_value = @value_value";

                    cmd.Parameters.AddWithValue("@account_id", accountId);
                    cmd.Parameters.AddWithValue("@value_key", unsavedProp.Key);
                    cmd.Parameters.AddWithValue("@value_value", unsavedProp.SerializeToString());

                    cmd.ExecuteNonQuery();

                    cmd.Parameters.Clear();
                }
            }

            profile.UnsavedProperties.Clear();

            doneCallback.Invoke();
        }
    }
}