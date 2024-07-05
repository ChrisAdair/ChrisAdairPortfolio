using System;
using System.Collections.Generic;
using Barebones.MasterServer;
using MySql.Data.MySqlClient;

class SaveLoadDbMysql : ISaveDatabase
{
    string _connectionString;


    public SaveLoadDbMysql(string connectionString)
    {
        _connectionString = connectionString;
    }
    public void GetSave(string saveName, Action<ISaveData> callback)
    {
        
    }

    public void GetLevel(int index, Action<ILevelData> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using(MySqlCommand com = new MySqlCommand())
        {
            conn.Open();

            com.Connection = conn;
            com.CommandText = "SELECT * FROM level_selection WHERE level_uID = @index";
            com.Parameters.AddWithValue("@index", index);

            var reader = com.ExecuteReader();

            if (!reader.HasRows)
                callback.Invoke(null);

            reader.Read();
            SqlLevelData level = new SqlLevelData
            {
                LevelID = (int)reader["level_uID"],
                LevelName = reader["level_name"] as string,
                GrainNumber = (int)reader["grain_number"],
                FileName = reader["file_name"] as string
            };

            callback.Invoke(level);
        }

    }

    public void GetNumberOfLevels(Action<int> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;
            com.CommandText = "SELECT MAX(level_uID) FROM level_selection";

            var reader = com.ExecuteReader();

            if (!reader.HasRows)
                callback.Invoke(-1);

            reader.Read();
            callback.Invoke(reader.GetInt32(0));
        }
    }

    public void GetLevelProperties(int index, Action<ILevelProps> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;
            com.CommandText = "SELECT * FROM level_properties WHERE level_id = @index";
            com.Parameters.AddWithValue("@index", index);

            var reader = com.ExecuteReader();

            if (!reader.HasRows)
                callback.Invoke(null);
            
            reader.Read();
            SqlLevelProps props = new SqlLevelProps
            {
                CompletionScore = (float) reader["completion_score"],
                TimeLimit = (int)reader["time_limit"],
                MoveLimit = (int)reader["move_limit"],
                TargetScore = (float)reader["target_score"]
            };
            //Read the mechanics into the properties
            bool simAneal = false;
            bool elastic = false;
            List<int> frozen = new List<int>();
            simAneal = (int)reader["sim_annealing"] == 1 ? true : false;
            elastic = (int)reader["elastic"] == 1 ? true : false;
            reader.Close();
            com.CommandText = "SELECT * FROM frozen_grains WHERE level = @index";
            reader = com.ExecuteReader();
            while(reader.Read())
            {
                frozen.Add((int)reader["frozen_grain"]);
            }

            props.Completion = GetCompleteCondition(props.TimeLimit, props.MoveLimit, props.TargetScore);
            props.Mechanics = GetMechanicCondition(simAneal, elastic, frozen);
            props.FrozenGrains = frozen;
            callback.Invoke(props);
        }
    }

    public void GetRaidLevel(int index, Action<IRaidData> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;
            com.CommandText = "SELECT * FROM raid_levels WHERE id = @index";
            com.Parameters.AddWithValue("@index", index);

            var reader = com.ExecuteReader();

            if (!reader.HasRows)
                callback.Invoke(null);

            reader.Read();
            SqlRaidLevel raidLevel = new SqlRaidLevel()
            {
                LevelID = (int)reader["id"],
                LevelName = reader["level_name"] as string,
                GrainNumber = (int)reader["grain_number"],
                FileName = reader["file_name"] as string,
                CompletionScore = (float)reader["completion_score"],
                TimeLimit = (int)reader["time_limit"],
                MoveLimit = (int)reader["move_limit"],
                TargetScore = (float)reader["target_score"],
                Description = reader["level_description"] as string
            };
            bool simAneal = false;
            bool elastic = false;
            List<int> frozen = new List<int>();
            simAneal = (int)reader["sim_annealing"] == 1 ? true : false;
            elastic = (int)reader["elastic"] == 1 ? true : false;
            reader.Close();
            com.CommandText = "SELECT * FROM frozen_grains WHERE level = @index";
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                frozen.Add((int)reader["frozen_grain"]);
            }

            raidLevel.Completion = GetCompleteCondition(raidLevel.TimeLimit, raidLevel.MoveLimit, raidLevel.TargetScore);
            raidLevel.Mechanics = GetMechanicCondition(simAneal, elastic, frozen);
            raidLevel.FrozenGrains = frozen;

            callback.Invoke(raidLevel);
        }
    }

    public void GetNumberOfCompLevels(Action<int> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;
            com.CommandText = "SELECT MAX(id) FROM comp_levels";

            var reader = com.ExecuteReader();

            if (!reader.HasRows)
                callback.Invoke(-1);

            reader.Read();
            callback.Invoke(reader.GetInt32(0));
        }
    }

    public void GetCompetitiveLevel(int index, Action<ICompetitiveData> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;
            com.CommandText = "SELECT * FROM comp_levels WHERE id = @index";
            com.Parameters.AddWithValue("@index", index);

            var reader = com.ExecuteReader();

            if (!reader.HasRows)
                callback.Invoke(null);

            reader.Read();
            SqlCompLevel compLevel = new SqlCompLevel()
            {
                LevelID = (int)reader["id"],
                LevelName = reader["level_name"] as string,
                GrainNumber = (int)reader["grain_number"],
                FileName = reader["file_name"] as string,
                CompletionScore = (float)reader["completion_score"],
                TimeLimit = (int)reader["time_limit"],
                MoveLimit = (int)reader["move_limit"],
                TargetScore = (float)reader["target_score"],
                Description = reader["level_description"] as string
            };
            bool simAneal = false;
            bool elastic = false;
            List<int> frozen = new List<int>();
            simAneal = (int)reader["sim_annealing"] == 1 ? true : false;
            elastic = (int)reader["elastic"] == 1 ? true : false;
            reader.Close();
            com.CommandText = "SELECT * FROM frozen_grains WHERE level = @index";
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                frozen.Add((int)reader["frozen_grain"]);
            }

            compLevel.Completion = GetCompleteCondition(compLevel.TimeLimit, compLevel.MoveLimit, compLevel.TargetScore);
            compLevel.Mechanics = GetMechanicCondition(simAneal, elastic, frozen);
            compLevel.FrozenGrains = frozen;

            callback.Invoke(compLevel);
        }
    }


    private CompletionCondition GetCompleteCondition(int time, int move, float target)
    {
        CompletionCondition cond = CompletionCondition.Score;

        if (time != 0)
        {
            cond = CompletionCondition.Time;
        }
        if (move != 0)
        {
            cond = cond | CompletionCondition.Moves;
        }
        if (target != 0)
        {
            cond = cond | CompletionCondition.ScoreTarget;
        }
        return cond;
    }

    private MechanicCondition GetMechanicCondition(bool simAneal, bool elastic, List<int> frozen)
    {
        MechanicCondition mech = MechanicCondition.None;
        if(simAneal)
        {
            mech = MechanicCondition.SimAnneal;
        }
        if(elastic)
        {
            mech = mech | MechanicCondition.Elastic;
        }
        if(frozen.Count>0)
        {
            mech = mech | MechanicCondition.Frozen;
        }
        return mech;
    }

    public void GetNumberOfChallLevels(Action<int> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;
            com.CommandText = "SELECT MAX(id) FROM chall_levels";

            var reader = com.ExecuteReader();

            if (!reader.HasRows)
                callback.Invoke(-1);

            reader.Read();
            callback.Invoke(reader.GetInt32(0));
        }
    }

    public void GetChallengeLevel(int index, Action<ICompetitiveData> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;
            com.CommandText = "SELECT * FROM chall_levels WHERE id = @index";
            com.Parameters.AddWithValue("@index", index);

            var reader = com.ExecuteReader();

            if (!reader.HasRows)
                callback.Invoke(null);

            reader.Read();
            SqlCompLevel compLevel = new SqlCompLevel()
            {
                LevelID = (int)reader["id"],
                LevelName = reader["level_name"] as string,
                GrainNumber = (int)reader["grain_number"],
                FileName = reader["file_name"] as string,
                CompletionScore = (float)reader["completion_score"],
                TimeLimit = (int)reader["time_limit"],
                MoveLimit = (int)reader["move_limit"],
                TargetScore = (float)reader["target_score"],
                Description = reader["level_description"] as string
            };

            bool simAneal = false;
            bool elastic = false;
            List<int> frozen = new List<int>();
            simAneal = (int)reader["sim_annealing"] == 1 ? true : false;
            elastic = (int)reader["elastic"] == 1 ? true : false;
            reader.Close();
            com.CommandText = "SELECT * FROM frozen_grains WHERE level = @index";
            reader = com.ExecuteReader();
            while (reader.Read())
            {
                frozen.Add((int)reader["frozen_grain"]);
            }

            compLevel.Completion = GetCompleteCondition(compLevel.TimeLimit, compLevel.MoveLimit, compLevel.TargetScore);
            compLevel.Mechanics = GetMechanicCondition(simAneal, elastic, frozen);
            compLevel.FrozenGrains = frozen;

            callback.Invoke(compLevel);
        }
    }

    public void GetLoadingHint(Action<string> callback)
    {
        using (MySqlConnection conn = new MySqlConnection(_connectionString))
        using (MySqlCommand com = new MySqlCommand())
        {
            conn.Open();
            com.Connection = conn;
            com.CommandText = "SELECT Hint_Text FROM Load_Hints ORDER BY RAND() LIMIT 1";

            var reader = com.ExecuteReader();

            if (!reader.HasRows)
                callback.Invoke(null);

            reader.Read();
            callback.Invoke(reader["Hint_Text"] as string);
        }
    }

    class SqlSaveLoadData : ISaveData
    {
        public int saveID { get; set; }


        public float score { get; set; }
    }


    class SqlLevelData : ILevelData
    {
        public int LevelID { get; set; }

        public string LevelName { get; set; }

        public int GrainNumber { get; set; }

        public string FileName { get; set; }
    }

    class SqlLevelProps : ILevelProps
    {
        public float CompletionScore { get; set; }
        public int TimeLimit { get; set; }
        public int MoveLimit { get; set; }
        public float TargetScore { get; set; }
        public CompletionCondition Completion { get; set; }
        public MechanicCondition Mechanics { get; set; }
        public List<int> FrozenGrains { get; set; }
    }

    class SqlRaidLevel : IRaidData
    {
        public int LevelID { get; set; }
        public string LevelName { get; set; }
        public int GrainNumber { get; set; }
        public string FileName { get; set; }
        public float CompletionScore { get; set; }
        public int TimeLimit { get; set; }
        public int MoveLimit { get; set; }
        public float TargetScore { get; set; }
        public CompletionCondition Completion { get; set; }
        public string Description { get; set; }
        public MechanicCondition Mechanics { get; set; }
        public List<int> FrozenGrains { get; set; }
    }

    class SqlCompLevel : ICompetitiveData
    {
        public int LevelID { get; set; }
        public string LevelName { get; set; }
        public int GrainNumber { get; set; }
        public string FileName { get; set; }
        public float CompletionScore { get; set; }
        public int TimeLimit { get; set; }
        public int MoveLimit { get; set; }
        public float TargetScore { get; set; }
        public CompletionCondition Completion { get; set; }
        public string Description { get; set; }
        public MechanicCondition Mechanics { get; set; }
        public List<int> FrozenGrains { get; set; }
    }
}

