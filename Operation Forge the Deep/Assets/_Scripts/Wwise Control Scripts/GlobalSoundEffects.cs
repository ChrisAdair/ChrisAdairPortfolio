using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalSoundEffects : MonoBehaviour
{
    public AK.Wwise.Event standardButton;
    public AK.Wwise.Event largeSplash;
    public AK.Wwise.Event smallSplash;
    public AK.Wwise.Event whaleSong;
    public AK.Wwise.Event loginSound;
    public AK.Wwise.Event metalCreak;
    public AK.Wwise.Event metalBreak;
    public AK.Wwise.Event metalStress;

    public static GlobalSoundEffects singleton;
    private void Start()
    {
        if(singleton != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            singleton = this;
        }
    }

    public void StandardButtonPress()
    {
        standardButton.Post(gameObject);
    }

    public void LargeSplash()
    {
        largeSplash.Post(gameObject);
    }

    public void SmallSplash()
    {
        smallSplash.Post(gameObject);
    }

    public void WhaleSong()
    {
        whaleSong.Post(gameObject);
    }

    public void LoginSound()
    {
        loginSound.Post(gameObject);
    }
    public void MetalCreak()
    {
        metalCreak.Post(gameObject);
    }
    public void MetalBreak()
    {
        metalBreak.Post(gameObject);
    }
    public void MetalStress()
    {
        metalStress.Post(gameObject);
    }
}
