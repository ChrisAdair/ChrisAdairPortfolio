using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Barebones.MasterServer;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    public Button Daily;
    public Button Monthly;
    public GameObject template;
    public GameObject content;
    public LeaderboardType leadType;
    public int levelID;

    void Start()
    {
        if(leadType == LeaderboardType.Raid)
        {
            Daily.onClick.AddListener(SetUpDailyLeaderboard);
            Monthly.onClick.AddListener(SetUpMonthlyLeaderboard);
        }
        else if(leadType == LeaderboardType.Competitive)
        {
            Daily.onClick.AddListener(SetUpDailyComp);
            Monthly.onClick.AddListener(SetUpMonthlyComp);
        }
    }

    
    public void SetUpDailyLeaderboard()
    {
        foreach(RectTransform child in content.transform)
        {
            Destroy(child.gameObject);
        }
        Msf.Client.Leader.GetDailyLeaderboardData(levelID, (packet) =>
        {
            foreach(string key in packet.userValues.Keys)
            {
                var entry = Instantiate(template, content.transform);
                entry.GetComponent<LeaderboardUIEntry>().users.text = key;
                //Highlight a row if it contains the current user
                if (key.Contains(Msf.Client.Auth.AccountInfo.Username))
                {
                    entry.GetComponent<LeaderboardUIEntry>().HighlightRow();
                }
                entry.GetComponent<LeaderboardUIEntry>().score.text = packet.userValues[key].ToString();
            }
        });
    }

    public void SetUpMonthlyLeaderboard()
    {
        foreach (RectTransform child in content.transform)
        {
            Destroy(child.gameObject);
        }
        Msf.Client.Leader.GetMonthlyLeaderboardData(levelID, (packet) =>
        {
            foreach (string key in packet.userValues.Keys)
            {
                var entry = Instantiate(template, content.transform);
                entry.GetComponent<LeaderboardUIEntry>().users.text = key;
                //Highlight a row if it contains the current user
                if (key.Contains(Msf.Client.Auth.AccountInfo.Username))
                {
                    entry.GetComponent<LeaderboardUIEntry>().HighlightRow();
                }
                entry.GetComponent<LeaderboardUIEntry>().score.text = packet.userValues[key].ToString();
            }
        });
    }

    //Be sure to set levelID before calling these methods
    public void SetUpDailyComp()
    {
        foreach (RectTransform child in content.transform)
        {
            Destroy(child.gameObject);
        }
        Msf.Client.Leader.GetDailyCompData(levelID, (packet) =>
        {
            foreach (string key in packet.userValues.Keys)
            {
                var entry = Instantiate(template, content.transform);
                entry.GetComponent<LeaderboardUIEntry>().users.text = key;
                //Highlight a row if it contains the current user
                if (key.Contains(Msf.Client.Auth.AccountInfo.Username))
                {
                    entry.GetComponent<LeaderboardUIEntry>().HighlightRow();
                }
                entry.GetComponent<LeaderboardUIEntry>().score.text = packet.userValues[key].ToString();
            }
        });
    }
    public void SetUpMonthlyComp()
    {
        foreach (RectTransform child in content.transform)
        {
            Destroy(child.gameObject);
        }
        Msf.Client.Leader.GetMonthlyCompData(levelID, (packet) =>
        {
            foreach (string key in packet.userValues.Keys)
            {
                var entry = Instantiate(template, content.transform);
                entry.GetComponent<LeaderboardUIEntry>().users.text = key;
                //Highlight a row if it contains the current user
                if (key.Contains(Msf.Client.Auth.AccountInfo.Username))
                {
                    entry.GetComponent<LeaderboardUIEntry>().HighlightRow();
                }
                entry.GetComponent<LeaderboardUIEntry>().score.text = packet.userValues[key].ToString();
            }
        });
    }
}
