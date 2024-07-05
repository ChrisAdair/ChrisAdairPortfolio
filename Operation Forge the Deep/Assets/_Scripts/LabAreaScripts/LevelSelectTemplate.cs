using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LevelSelectTemplate : MonoBehaviour
{
    public LevelSelectLabUI levelSelect;
    public TextMeshProUGUI levelName;
    public TextMeshProUGUI description;

    public Color selectedColor;
    public Color deselectedColor;
    public Image background;

    //Make this a property to update the highlighting when necessary
    public bool selected
    {
        get
        {
            return _selected;
        }
        set
        {
            _selected = value;
            if (_selected)
            {
                HighlightSelection();
            }
            else
            {
                Deselect();
            }
        }

    }

    public LoadLevelPacket level;
    public LevelPropsPacket props;

    private bool _selected;

    private void Awake()
    {
        _selected = false;
    }

    public void OnClick()
    {
        levelSelect.SelectLevel(this);
        if(levelSelect.selectionType == LevelSelectLabUI.LevelType.Competitive)
            levelSelect.UpdateLeaderboard(level.levelID);
    }

    private void HighlightSelection()
    {
        background.color = selectedColor;
    }
    private void Deselect()
    {
        background.color = deselectedColor;
    }
}
