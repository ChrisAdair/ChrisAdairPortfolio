using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AK.Wwise;
using UnityEngine.SceneManagement;

public class TriggerBedChange : MonoBehaviour
{

    public State mainMenu;
    public State epiZone;
    public State mezoZone;
    public State bathZone;
    public State labZone;
    public AK.Wwise.Event startMusic;
    public AK.Wwise.Event startLevel;
    public AK.Wwise.Event endLevelSuccess;
    public AK.Wwise.Event endLevelFailure;
    public AK.Wwise.Event getStar;
    public AK.Wwise.Event labAchieve; 

    public static TriggerBedChange singleton;

    private void Start()
    {
        if (singleton != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            DontDestroyOnLoad(this.gameObject);
            singleton = this;
            LevelTransition(0);
            startMusic.Post(gameObject);
            SceneManager.activeSceneChanged += OnSceneChanged;
        }
        
        
    }
    public void LevelTransition(int songID)
    {
        //song ID will determine the necessary bed to transition to

        switch (songID)
        {
            case 0:
                mainMenu.SetValue();
                break;
            case 1:
                epiZone.SetValue();
                break;
            case 2:
                mezoZone.SetValue();
                break;
            case 3:
                bathZone.SetValue();
                break;
            case 4:
                labZone.SetValue();
                break;
        }
    }

    private void OnSceneChanged(Scene previous, Scene next)
    {

        if(next.name != "Client" && next.name != "TheLab")
        {
            startLevel.Post(gameObject);
        }
        if(next.name == "Raid_Puzzle")
        {
            LevelTransition(4);
        }
    }

    public void EndLevelStinger(bool success)
    {
        if (success)
        {
            endLevelSuccess.Post(gameObject);
        }
        else
        {
            endLevelFailure.Post(gameObject);
        }
    }

    public void GotStar()
    {
        getStar.Post(gameObject);
    }
}
