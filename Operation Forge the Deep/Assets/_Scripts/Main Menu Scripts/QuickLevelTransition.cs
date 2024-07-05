using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuickLevelTransition : MonoBehaviour
{

    public void ChangeToLab()
    {
        SceneManager.LoadScene("TheLab");
    }

}
