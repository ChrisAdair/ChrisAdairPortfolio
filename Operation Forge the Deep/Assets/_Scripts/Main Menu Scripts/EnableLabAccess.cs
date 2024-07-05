using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Barebones.MasterServer;

public class EnableLabAccess : MonoBehaviour
{

    private int availableLevels;
    public List<CutsceneEnable> endCutscenes;
    private void Start()
    {
        var playerProfile = new ObservableProfile()
        {
            new ObservableInt(GrainPlayerProfileFactory.LevelProgress, 1),
            new ObservableDictStringFloat(GrainPlayerProfileFactory.highScores, new Dictionary<string, float>()),
            new ObservableDictStringFloat(GrainPlayerProfileFactory.highScoresLabChallenge, new Dictionary<string, float>())
        };

        Msf.Client.Profiles.GetProfileValues(playerProfile, (success, error) =>
        {
            if (!success)
                Logs.Error(error);

            availableLevels = playerProfile.GetProperty<ObservableInt>(GrainPlayerProfileFactory.LevelProgress).Value;
            if (availableLevels < 61)
            {
                gameObject.SetActive(false);
                GetComponent<UnityEngine.UI.Button>().interactable = false;
            }
            foreach(CutsceneEnable cut in endCutscenes)    
                cut.CheckEnabled();
        });

    }
}
