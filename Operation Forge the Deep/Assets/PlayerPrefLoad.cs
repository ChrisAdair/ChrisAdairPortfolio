using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using AK.Wwise;

public class PlayerPrefLoad : MonoBehaviour
{
    public Slider musicVol;
    public Slider sfxVol;
    public Slider voiceVol;
    public AudioMixer voiceMix;
    public GameObject mainCam;

    void Start()
    {
        musicVol.onValueChanged.AddListener(OnMusicvolChange);
        sfxVol.onValueChanged.AddListener(OnSFXvolChange);
        if (PlayerPrefs.HasKey("LevelSelectPos"))
        {
            mainCam?.transform.position.Set(mainCam.transform.position.x, PlayerPrefs.GetFloat("LevelSelectPos"), mainCam.transform.position.z);
        }
        if (PlayerPrefs.HasKey("MusicVol"))
        {
            musicVol.value = PlayerPrefs.GetFloat("MusicVol");
        }
        else
        {
            PlayerPrefs.SetFloat("MusicVol", 5.0f);
        }
        if (PlayerPrefs.HasKey("SFXVol"))
        {
            sfxVol.value = PlayerPrefs.GetFloat("SFXVol");
        }
        else
        {
            PlayerPrefs.SetFloat("SFXVol", 5.0f);
        }
        if (PlayerPrefs.HasKey("VoiceVol"))
        {
            voiceVol.value = PlayerPrefs.GetFloat("VoiceVol");
        }
        else
        {
            PlayerPrefs.SetFloat("VoiceVol", 5.0f);
        }
        Application.quitting -= PlayerPrefs.Save;
        Application.quitting += PlayerPrefs.Save;
    }
    public void OnMusicvolChange(float value)
    {
        AkSoundEngine.SetRTPCValue("MusicParam", value);
        PlayerPrefs.SetFloat("MusicVol", value);
        
    }
    public void OnSFXvolChange(float value)
    {
        AkSoundEngine.SetRTPCValue("SFXParam", value);
        PlayerPrefs.SetFloat("SFXVol", value);
    }
    public void OnVoicevolChange(float value)
    {
        voiceMix.SetFloat("VoiceVol",value);
        PlayerPrefs.SetFloat("VoiceVol", value);
    }
}
