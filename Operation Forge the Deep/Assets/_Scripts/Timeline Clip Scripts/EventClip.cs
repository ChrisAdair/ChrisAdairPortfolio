using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class EventClip : PlayableAsset, ITimelineClipAsset
{

    public EventBehavior template = new EventBehavior();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<EventBehavior>.Create(graph, template);

        return playable;

    }
}