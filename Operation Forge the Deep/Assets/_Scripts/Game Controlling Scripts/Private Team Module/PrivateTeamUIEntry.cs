using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PrivateTeamUIEntry : MonoBehaviour
{

    public TextMeshProUGUI username;
    public Image leaderCrown;
    public Button settings;

    void Start()
    {
        settings.onClick.AddListener(SetSettings);
    }

    public void SetLeader(bool leader)
    {
        if (leader)
        {
            leaderCrown.gameObject.SetActive(true);
        }
        else
        {
            leaderCrown.gameObject.SetActive(false);
        }
    }

    public void SetSettings()
    {

    }
}
