using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Networking;

[Serializable]
public class EventBehavior : PlayableBehaviour {

    public enum EventTrigger
    {
        cameraMove,
        rotateGrain,
        createConnection,
        getScore,
        changeGrain,
        showOptimization
    }

    public EventTrigger trigger;
    [Tooltip("If the object to listen to is in the scene, drag it here")]
    public GameObject target;
    public List<GrainConnection> targetConnections;
    [Tooltip("If the object is not in the scene until playtime, use this option")]
    public string targetName;
    public string hintText;


    private PlayableDirector director;
    private bool clipPlayed = false;
    private bool restartScheduled = false;
    private bool eventTriggered = false;
    private PlayerBehavior player;
    private int repetitions = 0;

    public override void OnPlayableCreate(Playable playable)
    {
        director = (playable.GetGraph().GetResolver() as PlayableDirector);
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        //First time execution
        if (!clipPlayed
            && info.weight > 0f)
        {
            //Set event to listen to here
            switch (trigger)
            {
                case EventTrigger.rotateGrain:
                    if (player == null)
                        player = GameObject.Find(targetName).GetComponent<PlayerBehavior>();
                    if (player == null)
                    {
                        Debug.LogError("Could not find the player object for tutorial event");
                        break;
                    }
                    else
                    {
                        target = player.assignedGrain.gameObject;
                        target.transform.hasChanged = false;
                    }    
                    break;
                case EventTrigger.cameraMove:
                    if (target == null)
                        target = GameObject.Find(targetName);
                    if (target == null) break;
                    target.transform.hasChanged = false;

                    break;
                case EventTrigger.createConnection:
                    if (player == null)
                        player = GameObject.Find(targetName).GetComponent<PlayerBehavior>();
                    if (player == null)
                    {
                        Debug.LogError("Could not find the player object for tutorial event");
                        break;
                    }
                    else
                    {
                        player = GameObject.Find(targetName).GetComponent<PlayerBehavior>();
                        targetConnections = player.assignedGrain.connections;
                    }

                    break;
                case EventTrigger.changeGrain:
                    if (player == null)
                        player = GameObject.Find(targetName).GetComponent<PlayerBehavior>();
                    if (player == null)
                    {
                        Debug.LogError("Could not find the player object for tutorial event");
                        break;
                    }
                    else
                    {
                        target = player.assignedGrain.gameObject;
                    }
                    break;
                case EventTrigger.showOptimization:
                    if (player == null)
                        player = GameObject.Find(targetName).GetComponent<PlayerBehavior>();
                    if (player == null)
                    {
                        Debug.LogError("Could not find the player object for tutorial event");
                        break;
                    }
                    else
                    {
                        target = player.assignedGrain.gameObject;
                        player.CmdOptimizeGrain(target);
                    }
                    break;
                case EventTrigger.getScore:
                    if (target == null)
                        target = GameObject.FindGameObjectWithTag("GameController");
                    if (target == null)
                    {
                        Debug.LogError("Could not find the player object for tutorial event");
                        break;
                    }
                    break;
            }

            if (Application.isPlaying)
            {
                    restartScheduled = true;
            }

            clipPlayed = true;
        }

        //Check if event was triggered
        switch (trigger)
        {
            case EventTrigger.rotateGrain:

                eventTriggered = eventTriggered || target.transform.hasChanged;
                if(eventTriggered)
                    restartScheduled = false;
                break;

            case EventTrigger.cameraMove:
                eventTriggered = eventTriggered || target.transform.hasChanged;
                if (eventTriggered)
                    restartScheduled = false;
                break;

            case EventTrigger.createConnection:
                //This always happens on the server, so the list doesn't exist on the client
                var max = targetConnections[0].MaxYScale;
                eventTriggered = eventTriggered || targetConnections[0].transform.localScale.y >= max*0.75f;
                if (eventTriggered)
                    restartScheduled = false;
                break;

            case EventTrigger.changeGrain:
                eventTriggered = eventTriggered || target != player.assignedGrain.gameObject;
                if (eventTriggered)
                    restartScheduled = false;
                break;
            case EventTrigger.showOptimization:
                restartScheduled = false;
                break;
            case EventTrigger.getScore:
                if(target.GetComponent<GrainNetworkAssigner>().SGTOutput > 5.0f)
                {
                    restartScheduled = false;
                }
                break;
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (restartScheduled)
        {
            restartScheduled = false;
            //Pause the timeline here
            //TutorialManager.singleton.PauseTimelineAction(director);
            repetitions++;
            if (repetitions > 2)
                TutorialManager.singleton.DisplayHintBox(true, hintText);
            director.time -= playable.GetTime();
        }
        else
        {
            //Turn off event listening here
            TutorialManager.singleton.DisplayHintBox(false, "");
            
        }
        clipPlayed = false;

    }
#if !UNITY_EDITOR


#endif
}
