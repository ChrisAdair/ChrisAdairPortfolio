using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;

public class LobbyPlayerBehavior : NetworkBehaviour {

    [SerializeField] Button ready;
    [SerializeField] NetworkLobbyPlayer lPlayer;
    public string[] saveFiles;
    public Dropdown dropdown;

	// Use this for initialization
	void Start () {
        ready = GameObject.Find("Ready").GetComponent<Button>();
        lPlayer = GetComponent<NetworkLobbyPlayer>();
        if (isLocalPlayer)
        {
            ready.onClick.AddListener(lPlayer.SendReadyToBeginMessage);
            //GetComponent<SaveFunctions>().CmdGetSaveFiles();
            saveFiles = GetComponent<SaveFunctions>().saveFiles;
            dropdown = GameObject.Find("Dropdown").GetComponent<Dropdown>();
            //To give time for network to fetch the save file locations, invoke after a certain time
            Invoke("GetSaveFiles", 0.25f);
        }
            
	}
	
    private void GetSaveFiles()
    {
        dropdown.AddOptions(saveFiles.ToList<string>());
        dropdown.AddOptions(new List<string>() { "New Game" });
        dropdown.onValueChanged.AddListener(CmdSetStructureToLoad);
        CmdSetStructureToLoad(dropdown.value);
    }

    [Command]
    public void CmdSetStructureToLoad(int idx)
    {
        LobbyManagerBehavior.currentStructureToLoad = dropdown.options[idx].text;
        RpcSetStructureToLoad(idx);

    }
    [ClientRpc]
    public void RpcSetStructureToLoad(int idx)
    {
        if(isLocalPlayer)
            dropdown.value = idx;
    }
}
