using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUIEntry : MonoBehaviour
{

    public TextMeshProUGUI users;
    public TextMeshProUGUI score;
    public Color highlightRow;
    public Image background;

    public void HighlightRow()
    {
        background.color = highlightRow;
    }

}
