using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RaidLobbyUserUI : MonoBehaviour
{
    private string _username;
    public TextMeshProUGUI displayUser;

    public string username
    {
        get { return _username; }
        set
        {
            _username = value;
            displayUser.text = value;
        }
    }

}
