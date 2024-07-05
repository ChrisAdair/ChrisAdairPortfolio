using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILeaderboard
{

    Dictionary<string, float> UserValues { get; set; }
    LeaderboardType LeadType { get; set; }

}
