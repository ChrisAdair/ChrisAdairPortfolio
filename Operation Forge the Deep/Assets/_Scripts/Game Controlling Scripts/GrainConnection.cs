using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using AK.Wwise;


public class GrainConnection : NetworkBehaviour {

    public Orientation[] pair;
    public Vector3 origin;
    public GrainMesher face;
    public Func<Quaternion, Quaternion, Vector3, double> diffModel;

    public AK.Wwise.Event connectionSound;
    //Values for lerps
    public float zScale = 0.5f;
    public float minDiam = 0.5f;
    public float diamGain = 0.5f;
    public float minScale = 0.5f;
    public double maxDiffusivity;
    private bool firstConnect;


    public float MaxYScale
    {
        get
        {
            Vector3 mid = (pair[0].transform.position + pair[1].transform.position) / 2.0f;
            
            return (mid - origin).magnitude/2.0f;
        }
    }



    public void Awake()
    {
        pair = new Orientation[2];
        origin = Vector3.zero;
        //Placeholder model
        maxDiffusivity = Math.Pow(10, 7) * (6.28 + Mathf.Sqrt(3)) + 1;
        //Read-Shockley Model
        //maxDiffusivity = 1;
        firstConnect = false;
    }

    public void Start()
    {
        ChangeSize();
    }
    public void ChangeSize()
    {
        //CHANGE if change the energy model
        double currDiff = face.diffusivity;
        float goal = (float) (currDiff / maxDiffusivity);

        if(goal <= 0.75)
        {
            firstConnect = false;
            var prevScale = transform.localScale.y;
            float scaleGoal = MaxYScale * goal / 0.75f;
            scaleGoal = scaleGoal > minScale ? scaleGoal : minScale;
            transform.localScale = new Vector3(minDiam, scaleGoal, minDiam);
            float deltaScale = transform.localScale.y - prevScale;
            SetScale(new Vector3(minDiam, scaleGoal, minDiam), deltaScale);
        }  
        else
        {
            if (!firstConnect && isClient)
            {
                connectionSound.Post(gameObject);
                firstConnect = true;
            }
            if(!firstConnect && isServer)
            {
                PlayConnectionSound();
                firstConnect = true;
            }
            //Max out the connection length, scale the diameter
            var prevScale = transform.localScale.y;
            
            float diamGoal = diamGain * (goal - 0.75f) / 0.25f + minDiam;

            
            transform.localScale = new Vector3(diamGoal, MaxYScale, diamGoal);
            float deltaScale = transform.localScale.y - prevScale;
            SetScale(new Vector3(diamGoal, MaxYScale, diamGoal), deltaScale);

        }
            
    }

    public void SetScale(Vector3 scale, float deltaScale)
    {
        transform.localScale = scale;
        transform.Translate(0, deltaScale, 0);
    }
    [Client]
    private void PlayConnectionSound()
    {
        connectionSound.Post(gameObject);
    }
}
