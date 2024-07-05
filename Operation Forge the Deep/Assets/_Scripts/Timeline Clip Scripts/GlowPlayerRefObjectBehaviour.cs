using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.EventSystems;

[Serializable]
public class GlowPlayerRefObjectBehaviour : PlayableBehaviour
{

    public GlowPlayerRefObjectClip.PlayerRefTarget TargetObject;
    public Material glowMaterial;
    public float glowTime;
    private Orientation set;
    private List<GrainConnection> conn;
    private Material original;
    private List<Material> originalConn = new List<Material>();
    private bool firstFrame = true;
    public override void OnPlayableCreate (Playable playable)
    {

    }
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        PlayerBehavior trackBinding = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerBehavior>();
        if (!trackBinding)
            return;
        if (!trackBinding.isClient) return;
        if (playable.IsDone())
        {
            switch (TargetObject)
            {
                case GlowPlayerRefObjectClip.PlayerRefTarget.assignedGrain:
                    set.gameObject.GetComponent<MeshRenderer>().material = original;
                    break;
                case GlowPlayerRefObjectClip.PlayerRefTarget.connection:
                    //trackBinding.gameObject.GetComponent<ColorOrientations>().CmdChangeConnectionColor();
                    break;
            }
            return;
        }
        float blend = 0;
        // Use the above variables to process each frame of this playable.
        switch (TargetObject)
        {
            case GlowPlayerRefObjectClip.PlayerRefTarget.assignedGrain:
                if (firstFrame)
                {
                    set = trackBinding.assignedGrain;
                    original = new Material(set.gameObject.GetComponent<MeshRenderer>().material);
                    firstFrame = false;
                }
                blend = Mathf.PingPong((float)playable.GetTime(), glowTime) / glowTime;
                Material blending = new Material(original);
                blending.Lerp(original, glowMaterial, blend);
                set.gameObject.GetComponent<MeshRenderer>().material = blending;
                break;
            case GlowPlayerRefObjectClip.PlayerRefTarget.connection:
                if (firstFrame)
                {
                    conn = trackBinding.assignedGrain.connections;
                    foreach (GrainConnection iter in conn)
                    {
                        originalConn.Add(new Material(iter.gameObject.GetComponent<MeshRenderer>().material));
                    }
                    firstFrame = false;
                }
                blend = Mathf.PingPong((float)playable.GetTime(), glowTime) / glowTime;
                for (int j = 0; j < conn.Count; j++)
                {
                    conn[j].gameObject.GetComponent<MeshRenderer>().material.Lerp(originalConn[j], glowMaterial, blend);
                }
                break;
        }
    }
}
