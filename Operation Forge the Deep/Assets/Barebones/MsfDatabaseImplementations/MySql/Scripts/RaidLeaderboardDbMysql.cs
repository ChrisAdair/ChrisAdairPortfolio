using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySql.Data.MySqlClient;
using System;

public class RaidLeaderboardDbMysql : ILeaderboardDatabase
{

    private string _connectionString;

    public RaidLeaderboardDbMysql(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void GetDailyLeaderboard(int levelID, Action<ILeaderboard> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;

            com.CommandText = "SELECT * FROM raid_leaderboard WHERE level_id  = @levelID AND " +
                              "DATE(date_completed) = UTC_DATE " +
                              "ORDER BY score DESC";
            com.Parameters.AddWithValue("@levelID", levelID);

            var reader = com.ExecuteReader();

            var leaderboard = new RaidLeaderboard()
            {
                LeadType = LeaderboardType.Raid,
                UserValues = new Dictionary<string, float>()
            };
            while (reader.Read())
            {
                leaderboard.UserValues.Add((string)reader["player_list"], (float)reader["score"]);
            }

            callback.Invoke(leaderboard);

            reader.Close();
        }
    }

    public void GetMonthlyLeaderboard(int levelID, Action<ILeaderboard> callback)
    {
        //TODO need a level ID for leaderboard retrieval
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;
            com.CommandText = "SELECT * FROM raid_leaderboard WHERE level_id  = @levelID AND " +
                              "MONTH(date_completed) = MONTH(UTC_DATE) AND YEAR(date_completed) = YEAR(UTC_DATE) " +
                              "ORDER BY score DESC";
            com.Parameters.AddWithValue("@levelID", levelID);

            var reader = com.ExecuteReader();

            var leaderboard = new RaidLeaderboard()
            {
                LeadType = LeaderboardType.Raid,
                UserValues = new Dictionary<string, float>()
            };
            while (reader.Read())
            {
                leaderboard.UserValues.Add((string)reader["player_list"], (float)reader["score"]);
            }

            callback.Invoke(leaderboard);

            reader.Close();
        }
    }

    public void PostScore(PostScorePacket packet, Action<bool> callback)
    {
        try
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            using (MySqlCommand com = new MySqlCommand())
            {
                conn.Open();
                com.Connection = conn;
                var dateTime = DateTime.FromFileTimeUtc(packet.fileTime);

                com.CommandText = "update raid_leaderboard set score = @score, date_completed = @date where player_list = @playerList and level_id = @levelID and @score > score;" + 
                                  " insert into raid_leaderboard(level_id, player_list, score, date_completed) select @levelID as level_id, @playerList, @score, @date from raid_leaderboard" + 
                                  " where level_id = @levelID and player_list = @playerList having count(*) = 0;";
                com.Parameters.AddWithValue("@levelID", packet.levelID);
                com.Parameters.AddWithValue("@playerList", packet.username);
                com.Parameters.AddWithValue("@score", packet.score);
                com.Parameters.AddWithValue("@date", dateTime.ToString("s"));

                com.ExecuteNonQuery();
                callback.Invoke(true);
                conn.Close();
            }
        }
        catch(MySqlException e)
        {
            Logs.Error("Could not submit score to leaderboard: " + e.ToString());
            callback.Invoke(false);
        }
        
    }

    private class RaidLeaderboard : ILeaderboard
    {
        public Dictionary<string, float> UserValues { get; set; }
        public LeaderboardType LeadType { get; set; }
    }
}
