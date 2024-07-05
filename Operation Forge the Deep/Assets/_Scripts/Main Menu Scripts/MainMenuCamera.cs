using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class MainMenuCamera : MonoBehaviour
{
    [Header("Set in Inspector")]
    [Tooltip("This will list the locations the camera can move to on the menu")]
   
    public Camera cameraMain;
    public float scaling;
    public GameObject LevelSelect;
    public float upperBound;
    public float lowerBound;
    public float scale;
    
    // Initialize all of the different variables
    void Start()
    {
        if (cameraMain == null)
        {
            cameraMain = Camera.main;
        }
        if (PlayerPrefs.HasKey("LevelSelectPos"))
        {
            transform.position = new Vector3(transform.position.x, PlayerPrefs.GetFloat("LevelSelectPos"), transform.position.z);
            Debug.Log("Set camera start to: " + PlayerPrefs.GetFloat("LevelSelectPos"));
        }
    }

    private void Update()
    {
        if(Input.mouseScrollDelta.y != 0)
        {
            Vector3 newPosition = new Vector3(transform.position.x, Mathf.Clamp(Input.mouseScrollDelta.y * scale + transform.position.y, lowerBound, upperBound), transform.position.z);
            transform.position = newPosition;
        }
    }
    private void OnDisable()
    {
        PlayerPrefs.SetFloat("LevelSelectPos", transform.position.y);
        Debug.Log("Set value to: " + transform.position.y);
    }
}
