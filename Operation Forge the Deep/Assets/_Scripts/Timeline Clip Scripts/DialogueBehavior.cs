using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

[Serializable]
public class DialogueBehavior : PlayableBehaviour {

    public string characterName;
    public string dialogueLine;
    public int dialogueSize;

    public bool hasToPause = false;
    public bool displayLeftCharacter = true;
    public bool displayRightCharacter = true;
    public Sprite LeftCharacterSprite;
    public Sprite RightCharacterSprite;

    private bool clipPlayed = false;
    private bool pauseScheduled = false;
    private PlayableDirector director;

    public override void OnPlayableCreate(Playable playable)
    {
        director = (playable.GetGraph().GetResolver() as PlayableDirector);
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!clipPlayed
            && info.weight > 0f)
        {
            TutorialManager.singleton.skipClip = false;
            //Set and display UI here
            TutorialManager.singleton.SetDialogueData(characterName, dialogueLine, dialogueSize);
            if (displayLeftCharacter)
            {
                if (LeftCharacterSprite != null)
                    TutorialManager.singleton.ToggleLeftCharacterUI(true, LeftCharacterSprite);
                else
                    TutorialManager.singleton.ToggleLeftCharacterUI(false);
            }
            else
                TutorialManager.singleton.ToggleLeftCharacterUI(false);
            if (displayRightCharacter)
            {
                if (RightCharacterSprite != null)
                    TutorialManager.singleton.ToggleRightCharacterUI(true, RightCharacterSprite);
                else
                    TutorialManager.singleton.ToggleRightCharacterUI(false);
            }
            else
                TutorialManager.singleton.ToggleRightCharacterUI(false);
            if (Application.isPlaying)
            {
                if (hasToPause)
                {
                    pauseScheduled = true;
                }
            }
            
            clipPlayed = true;
        }
        if(TutorialManager.singleton.skipClip)
        {
            director.time += playable.GetDuration() - playable.GetTime();
            pauseScheduled = false;
        }
    }
    
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (pauseScheduled)
        {
            pauseScheduled = false;
            //Pause the timeline here
            TutorialManager.singleton.PauseTimelineButton(director);

        }
        else
        {
            //Turn off Dialoge box here
            
            //TutorialManager.singleton.ToggleLeftCharacterUI(false);
            //TutorialManager.singleton.ToggleRightCharacterUI(false);
        }

        clipPlayed = false;
    }
}
