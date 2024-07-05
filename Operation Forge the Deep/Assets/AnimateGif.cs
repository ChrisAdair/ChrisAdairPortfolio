using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimateGif : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond;
    public Image source;
    private int index = 0;
    float timeCurr = 0f;

    void Update()
    {
        source.sprite = frames[index];
        timeCurr += Time.deltaTime;
        if(timeCurr >= 1/framesPerSecond)
        {
            timeCurr = 0;
            index++;
            if (index >= frames.Length)
            {
                index = 0;
            }
        }
        
    }
}
