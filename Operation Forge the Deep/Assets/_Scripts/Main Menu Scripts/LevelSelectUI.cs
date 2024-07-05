using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;
using Barebones.MasterServer;
using UnityEngine.UI;

public class LevelSelectUI : MonoBehaviour {

    public GameObject item;
    public List<LevelButton> levels;
    public List<CutsceneEnable> cutscenes;
    public ObservableProfile playerProfile;
    public LevelDescription levelDescription;
    public int levelSetStartIndex;
    public GameObject ShadowUnlock;

    private int availableLevels;

    public void ToggleActiveGameObject()
    {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
    private void OnEnable()
    {
        //Create a default profile to be filled in
        playerProfile = new ObservableProfile()
        {
            new ObservableInt(GrainPlayerProfileFactory.LevelProgress, 1),
            new ObservableDictStringFloat(GrainPlayerProfileFactory.highScores, new Dictionary<string, float>()),
            new ObservableDictStringFloat(GrainPlayerProfileFactory.highScoresLabChallenge, new Dictionary<string, float>())
        };
        
        Msf.Client.Profiles.GetProfileValues(playerProfile, (success, error) =>
        {
            if(!success)
                Logs.Error(error);

            availableLevels = playerProfile.GetProperty<ObservableInt>(GrainPlayerProfileFactory.LevelProgress).Value;
            Msf.Client.SaveLoad.GetNumberOfLevels((response) =>
            {
                if (response < 0)
                {
                    Debug.LogError("Error finding number of levels: Code " + response);
                    return;
                }
                else
                    PopulateLevelButtons(response);

            });
        });
    }

    private void PopulateLevelButtons(int numLevels)
    {
        int i = 0;
        bool atLeastOneActive = false;
        foreach(LevelButton levelData in levels)
        {
            Msf.Client.SaveLoad.GetLevelData(levelSetStartIndex + i + 1, (data, error) =>
            {
                if (data == null)
                {
                    Debug.LogError(error);
                    return;
                }
                
                levelData.buttonName.text = data.levelName;
                levelData.levelName = data.levelName;
                levelData.fileName = data.fileName;
                levelData.levelID = data.levelID;
                levelData.levelDescription = levelDescription;
                levelData.SetButtonProperties();
                //Set the activation of levels here for play based off of the player's progress
                //Set cutscene to autoplay at correct levels here
                if (data.levelID <= availableLevels)
                {
                    levelData.gameObject.SetActive(true);
                    levelData.button.interactable = true;
                    atLeastOneActive = true;
                }
                else
                {
                    levelData.gameObject.SetActive(true);
                    levelData.button.interactable = false;
                    try { levelData.triggeredCutscene.GetComponent<Button>().interactable = false; } catch { }
                    
                }
                if(data.levelID == availableLevels)
                {
                    try
                    {
                        levelData.triggeredCutscene.onClick.Invoke();
                    }
                    catch { }
                }
                if (atLeastOneActive)
                    ShadowUnlock.SetActive(true);
            });
            i++;
        }
        foreach(CutsceneEnable cut in cutscenes)
        {
            cut.CheckEnabled();
        }
        
        StartCoroutine(WaitForLoadScreen());
        
    }
    private IEnumerator WaitForLoadScreen()
    {
        yield return new WaitForSeconds(0.5f);
        LoadingScreen.singleton.fade = false;
    }
}
