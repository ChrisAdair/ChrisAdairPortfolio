using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Barebones.MasterServer;
using System.IO;
using UnityEngine.UI;
using Barebones.Networking;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Rendering.PostProcessing;

public class StartHostValues : MonoBehaviour
{
    public List<Material> allSkyboxes;
    public List<PostProcessVolume> allPostProc;
    [Tooltip("Enter in the different x,y,z positions for the background")]
    public List<Vector3> allCliffPositions;
    [Tooltip("Drag the parent object of the background here so it will be moved to the right spot")]
    public GameObject backgroundParentObject;
    [Tooltip("Drag the lighting objects for each level here")]
    public List<GameObject> individualLevelLighting;
    [Tooltip("Set the colors for the fog on each level")]
    public List<Color> levelFogColors;
    public List<Material> CubeMaterials;
    public List<GameObject> EuphoticEnable;
    public List<GameObject> EuphoticDisable;
    public List<GameObject> DysphoticEnable;
    public List<GameObject> DysphoticDisable;
    public List<GameObject> AphoticEnable;
    public List<GameObject> AphoticDisable;
    public List<GameObject> LabEnable;
    public List<GameObject> LabDisable;
    public List<float> fogEndDistance;
    public List<Material> highlightCubeMat;

    [Header("Set Dynamically")]
    public Skybox zoneSkybox;


}
