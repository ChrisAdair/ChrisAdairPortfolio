using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Barebones.MasterServer;

public class ReadyCheckUI : MonoBehaviour
{
    public TextMeshProUGUI readyMessage;
    public Sprite notReady;
    public Sprite ready;
    public Button readyButton;
    public GameObject readyIconPrefab;
    public List<GameObject> readyIcons;

    private void Start()
    {
        readyButton.onClick.AddListener(SendReadyCheck);
    }

    public void SetReadyState(List<bool> readyList, string leader)
    {
        readyMessage.text = leader + " wants to start a level";
        if(readyIcons.Count < readyList.Count)
        {
            var diff = readyList.Count - readyIcons.Count;
            for (int i = 0; i < diff; i++)
            {
                GameObject newIcon = Instantiate(readyIconPrefab, transform);
                readyIcons.Add(newIcon);
            }
        }
        else if(readyIcons.Count > readyList.Count)
        {
            var diff = readyIcons.Count - readyList.Count;
            for (int i = 0; i < diff; i++)
            {
                Destroy(readyIcons[readyIcons.Count - 1]);
                readyIcons.RemoveAt(readyIcons.Count - 1);
            }
        }
        SetReadyObjects(readyList);
    }

    private void SetReadyObjects(List<bool> readyList)
    {
        for(int i = 0; i < readyList.Count; i++)
        {
            if (readyList[i])
            {
                readyIcons[i].GetComponent<Image>().sprite = ready;
            }
            else
            {
                readyIcons[i].GetComponent<Image>().sprite = notReady;
            }
        }
    }

    private void SendReadyCheck()
    {
        Msf.Client.Team.SendReadyCheck();
    }
}
