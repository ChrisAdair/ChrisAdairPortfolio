using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Barebones.MasterServer;
using Barebones.Networking;

public class RandomMatchmakeButton : MonoBehaviour {

    public Button matchmakeButton;
    public Button cancelMatchmakingBtn;
    public GameObject findingGamePanel;
    public Text findingGameText;
    public Image rotatingImage;
    public bool SetAsLastSiblingOnEnable;

	void Start () {

        
	}

    private void Update()
    {
        if(findingGamePanel.activeInHierarchy)
            rotatingImage.transform.Rotate(Vector3.forward, Time.deltaTime * 360 * 2);
    }
    public void OnEnable()
    {
        if (SetAsLastSiblingOnEnable)
            transform.SetAsLastSibling();
    }

    public void StartMatchmaking()
    {
        if(!Msf.Client.Auth.IsLoggedIn)
        {
            Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateError("You are not currently logged in"));
        }
        else
        {
            
            //Setting display for game finding
            matchmakeButton.interactable = false;
            cancelMatchmakingBtn.interactable = true;
            findingGamePanel.SetActive(true);
            findingGameText.text = "Finding game ...";

            //Send server message to queue up the player
            Msf.Client.MatchmakeClient.RandomMatchmake((gameInfo) => {
                
                if (gameInfo == null)
                {
                    Msf.Events.Fire(Msf.EventNames.ShowDialogBox, DialogBoxData.CreateInfo("Could not find a game, servers may be full"));
                    findingGamePanel.SetActive(false);
                }
                else
                {

                    findingGameText.text = "Found Game!";
                    Msf.Client.Rooms.GetAccess(gameInfo.Id, (access, error) =>
                    {
                        if (access == null)
                        {
                            Logs.Logger.Error("Error in accessing matchmade room: " + error);

                        }
                        findingGamePanel.SetActive(false);
                        matchmakeButton.interactable = true;
                    });
                }
                   
            }, Msf.Client.Connection);

            
        }
  
    }

    public void CancelMatchmaking()
    {
        //Set display properties

        cancelMatchmakingBtn.interactable = false;
        //Send the cancellation method to the server
        //Check if the request was cancelled

        Msf.Client.MatchmakeClient.CancelMatchmake((success) =>
        {
            if (success)
            {
                findingGamePanel.SetActive(false);
                matchmakeButton.interactable = true;
            }
            else
            {
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox, DialogBoxData.CreateError("Could not find user in matchmaking queue"));
                findingGamePanel.SetActive(false);
                matchmakeButton.interactable = true;
            }
            
        }, Msf.Client.Connection);
        
    }

}
