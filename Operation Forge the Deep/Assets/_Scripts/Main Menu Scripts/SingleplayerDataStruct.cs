using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Barebones.MasterServer;

public class SingleplayerDataStruct : MonoBehaviour
{

    public Dictionary<string, string> properties { get; set; }
    public AsyncOperation sceneLoading;
    private string offlineSceneName = "";

    // Start is called before the first frame update
    public void Initialize()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnSceneChanged;
        if(SceneManager.GetActiveScene().name != properties[MsfDictKeys.SceneName])
        {
            offlineSceneName = SceneManager.GetActiveScene().name;
        }
    }

    void OnSceneChanged(Scene scene1, Scene scene2)
    {
        if(scene2.name != properties[MsfDictKeys.SceneName])
        {
            Destroy(gameObject);
        }
        else
        {
            MinimalNetworkManager.singleton.offlineScene = offlineSceneName;
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
}
