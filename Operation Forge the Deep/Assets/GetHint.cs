using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Barebones.MasterServer;

public class GetHint : MonoBehaviour
{
    public TextMeshProUGUI hint;
    // Start is called before the first frame update
    private void OnEnable()
    {
        LoadingScreen.singleton.GetDatabaseHint();
        Msf.Client.SaveLoad.GetLoadingHint((pulledHint, error) =>
        {
            if(pulledHint !=null)
            {
                hint.text = pulledHint;
            }
            else
            {
                Debug.LogError(error);
            }
            
        });
    }
}
