using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Barebones.MasterServer;
using TMPro;

public class LoadingScreen : MonoBehaviour
{

    public float fadeTime = 2f;
    public float fadeInc = 0.01f;
    public GameObject background;
    public static LoadingScreen singleton;
    public TextMeshProUGUI hintText;
    public bool fade;
    public float progress;
    // Start is called before the first frame update
    void Awake()
    {
        if (singleton != null)
            Destroy(gameObject);
        singleton = this;
        DontDestroyOnLoad(gameObject);
    }
    public IEnumerator UnfadeScene(Scene scene)
    {
        while(!scene.isLoaded)
        {
            yield return null;
        }
        fade = false;
    }
    public void GetDatabaseHint()
    {
        Msf.Client.SaveLoad.GetLoadingHint((hint, response) =>
        {
            if(hint==null)
            {
                Debug.LogError(response);
            }
            else
            {
                hintText.text = hint;
            }
        });
    }
    private void Update()
    {
        if(fade)
        {
            //background.CrossFadeAlpha(1, fadeTime, false);
            background.SetActive(true);
        }
        if(!fade)
        {
            //background.CrossFadeAlpha(0, fadeTime, false);
            background.SetActive(false);
        }
    }
}
