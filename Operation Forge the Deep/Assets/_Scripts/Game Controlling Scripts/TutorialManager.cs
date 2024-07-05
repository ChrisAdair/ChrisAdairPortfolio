using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour {

    //Singleton behavior
    public static TutorialManager singleton;

    //Cutscene assets
    public PlayableDirector currentDirector;
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;
    public GameObject leftCharacter;
    public GameObject rightCharacter;
    public GameObject hintTextBox;
    public Image characterArt;
    public LevelCompletion gameCompletion;
    public bool isInGame;
    private bool pausedButton = false;
    private bool pausedAction = false;
    private bool eventsCleared = false;
    private PlayerBehavior.GameReady startCutscene;
    public bool skipClip = false;

	void Awake () {
        if (TutorialManager.singleton == null)
        {
            singleton = this;
        }
        else
            Debug.LogError("There should only be one singleton in the scene at a time!!!!");
	}
    private void Start()
    {
        if(isInGame)
        {
            startCutscene = new PlayerBehavior.GameReady(StartTimeline);

            PlayerBehavior.OnGameReady += startCutscene;
        }
        
    }
    private void OnDisable()
    {
        PlayerBehavior.OnGameReady -= startCutscene;
    }
    // Update is called once per frame
    void Update () {
		
        if(pausedButton && Input.GetMouseButtonDown(0))
        {
            ResumeTimelineButton();
        }
        else if(Input.GetMouseButtonDown(0) && currentDirector.state == PlayState.Playing)
        {
            skipClip = true;
        }
	}
    public void StartTimeline()
    {
        currentDirector.Play();
        gameCompletion = GameObject.FindObjectOfType<LevelCompletion>();
        gameCompletion.PauseScoreListen();
    }
    public void EnableLevelCompletion(bool enabled)
    {
        gameCompletion.PauseScoreListen();
    }
    public void PauseTimelineButton(PlayableDirector director)
    {
        currentDirector = director;

        director.playableGraph.GetRootPlayable(0).SetSpeed(0);
        pausedButton = true;
    }
    public void ResumeTimelineButton()
    {
        pausedButton = false;

        currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(1);

    }
    public void PauseTimelineAction(PlayableDirector director)
    {
        currentDirector = director;
        director.playableGraph.GetRootPlayable(0).SetSpeed(0);
        pausedAction = true;
        
    }

    public void ResumeTimelineAction()
    {
        currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(1);
    }

    public void SetDialogueData(string characterName, string dialogue, int textSize)
    {
        dialogueText.text = characterName + ": " + dialogue;
        dialogueText.fontSize = textSize;
        ToggleDialogueUI(true);
    }
    public void ToggleDialogueUI(bool active)
    {
        dialogueBox.SetActive(active);
    }
    public void ToggleLeftCharacterUI(bool active)
    {
        leftCharacter.SetActive(active);
    }
    public void ToggleLeftCharacterUI(bool active, Sprite sprite)
    {
        leftCharacter.GetComponent<Image>().sprite = sprite;
        leftCharacter.SetActive(active);
    }
    public void ToggleRightCharacterUI(bool active)
    {
        rightCharacter.SetActive(active);
    }
    public void ToggleRightCharacterUI(bool active, Sprite sprite)
    {
        rightCharacter.GetComponent<Image>().sprite = sprite;
        rightCharacter.SetActive(active);
    }
    public void DisplayHintBox(bool active, string hintText)
    {
        hintTextBox.SetActive(active);
        hintTextBox.GetComponentInChildren<TextMeshProUGUI>().text = hintText;
    }
}
