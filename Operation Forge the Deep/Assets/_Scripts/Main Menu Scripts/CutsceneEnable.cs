using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

public class CutsceneEnable : MonoBehaviour
{
    public GameObject triggerObject;

    // Start is called before the first frame update
    public void CheckEnabled()
    {
        if (triggerObject == null)
            return;
        if(triggerObject.GetComponent<Button>().interactable)
        {
            gameObject.GetComponent<Button>().interactable = true;
        }
        else
            gameObject.GetComponent<Button>().interactable = false;

    }
    public void Autoplay()
    {
        triggerObject.GetComponent<PlayableDirector>().Play();
    }
}
