using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class LevelDescription : MonoBehaviour
{
    public TextMeshProUGUI levelDescripionBlock;
    public Button StartGame;
    public Button Cancel;
    public int zoneID;

    // Start is called before the first frame update
    void Awake()
    {
        Cancel.onClick.AddListener(()=> { gameObject.SetActive(false); });
    }

   public void SetInformation(string description, Action levelStart)
    {
        if(StartGame.onClick != null)
        {
            StartGame.onClick.RemoveAllListeners();
        }
        StartGame.onClick.AddListener(levelStart.Invoke);
        levelDescripionBlock.text = description;
        gameObject.SetActive(true);
        StartGame.onClick.AddListener(delegate { TriggerBedChange.singleton.LevelTransition(zoneID); });
        StartGame.onClick.AddListener(delegate { PlayerPrefs.SetFloat("LevelSelectPos", Camera.main.transform.position.y); });
    }
}
