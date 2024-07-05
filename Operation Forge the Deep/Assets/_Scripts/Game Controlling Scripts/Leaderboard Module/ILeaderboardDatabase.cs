using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface ILeaderboardDatabase
{

    void GetDailyLeaderboard(int levelId, Action<ILeaderboard> callback);
    void GetMonthlyLeaderboard(int levelId, Action<ILeaderboard> callback);
    void PostScore(PostScorePacket packet, Action<bool> callback);
}
