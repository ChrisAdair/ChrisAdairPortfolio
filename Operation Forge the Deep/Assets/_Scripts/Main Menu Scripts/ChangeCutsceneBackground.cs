using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeCutsceneBackground : MonoBehaviour
{
    public Image background;
    // Start is called before the first frame update
    void Start()
    {
        background = GetComponent<Image>();
    }

    public void SetBackground(Sprite newBackground)
    {
        if (!background.gameObject.activeSelf)
            background.gameObject.SetActive(true);
        background.sprite = newBackground;
    }
}
