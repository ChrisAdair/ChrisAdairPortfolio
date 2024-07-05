using System;
using System.Collections.Generic;
using Barebones.MasterServer;

interface ISaveDatabase
{


    void GetSave(string saveName, Action<ISaveData> callback);

    void GetLevel(int index, Action<ILevelData> callback);

    void GetNumberOfLevels(Action<int> callback);

    void GetLevelProperties(int index, Action<ILevelProps> callback);

    void GetRaidLevel(int index, Action<IRaidData> callback);

    void GetNumberOfCompLevels(Action<int> callback);

    void GetCompetitiveLevel(int index, Action<ICompetitiveData> callback);
    void GetNumberOfChallLevels(Action<int> callback);
    void GetChallengeLevel(int index, Action<ICompetitiveData> callback);
    void GetLoadingHint(Action<string> callback);
}

