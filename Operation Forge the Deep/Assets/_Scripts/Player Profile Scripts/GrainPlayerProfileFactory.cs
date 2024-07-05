using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;

public class GrainPlayerProfileFactory : ServerModuleBehaviour
{
    //Setting up the keys for profile database access
    public const int LevelProgress = 1;
    public const int highScores = 2; //Leads to dictionary of high scores for the main game
    public const int highScoresLabChallenge = 3; //Leads to dictionary of high scores for the lab

    private void Awake()
    {
        AddOptionalDependency<ProfilesModule>();
    }

    public override void Initialize(IServer server)
    {
        base.Initialize(server);

        var profilesModule = server.GetModule<ProfilesModule>();

        if (profilesModule == null)
            return;

        // Set the factory for creating new profiles
        profilesModule.ProfileFactory = CreateProfileInServer;

    }

    public static ObservableServerProfile CreateProfileInServer(string username, IPeer peer)
    {
        return new ObservableServerProfile(username, peer)
        {
            // Start with the fillable field of levels completed
            new ObservableInt(LevelProgress, 1),
            new ObservableDictStringFloat(highScores),
            new ObservableDictStringFloat(highScoresLabChallenge)
        };
    }

    public static ObservableServerProfile CreateProfileInServer(string username)
    {
        return CreateProfileInServer(username, null);
    }
}
