using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.EventSystems;

[Serializable]
public class GlowPlayerRefObjectClip : PlayableAsset, ITimelineClipAsset
{
    public GlowPlayerRefObjectBehaviour template = new GlowPlayerRefObjectBehaviour ();
    public enum PlayerRefTarget
    {
        assignedGrain,
        connection
    };
    public PlayerRefTarget TargetObject;
    public Material glowMaterial;
    public float glowTime;
    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<GlowPlayerRefObjectBehaviour>.Create (graph, template);
        GlowPlayerRefObjectBehaviour clone = playable.GetBehaviour ();
        clone.glowMaterial = glowMaterial;
        clone.glowTime = glowTime;
        clone.TargetObject = TargetObject;
        return playable;
    }
}
