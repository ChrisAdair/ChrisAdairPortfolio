using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using TMPro;

public class RaidLobbyUI : MonoBehaviour, ILobbyListener
{
    public JoinedLobby raidLobby;
    public GameObject UserUIPrefab;
    public TextMeshProUGUI statusText;
    public Dictionary<string, GameObject> userDisplay;

    private GameObject GeneratePlayerUI(string username)
    {
        GameObject playerUI = Instantiate(UserUIPrefab, transform);
        playerUI.GetComponent<RaidLobbyUserUI>().username = username;
        return playerUI;
    }

    private void StartGame()
    {
        raidLobby.GetLobbyRoomAccess((room, error) =>
        {
            if (room == null)
            {
                Debug.LogError("Error accessing created room: " + error);
            }

            if (RoomConnector.Instance != null)
                RoomConnector.Connect(room);
        });
    }

    public void LeaveLobby()
    {
        raidLobby.Leave();
    }

    private void OnDestroy()
    {
        if (raidLobby != null && raidLobby.Listener.Equals(this))
        {
            raidLobby.SetListener(null);
        }
    }
    #region LobbyListener
    public void Initialize(JoinedLobby lobby)
    {
        raidLobby = lobby;
        userDisplay = new Dictionary<string, GameObject>();
        foreach (LobbyMemberData member in raidLobby.Members.Values)
        {
            userDisplay.Add(member.Username, GeneratePlayerUI(member.Username));
        }

        gameObject.SetActive(true);
    }

    public void OnMemberPropertyChanged(LobbyMemberData member, string property, string value)
    {
        
    }

    public void OnMemberJoined(LobbyMemberData member)
    {
        userDisplay.Add(member.Username, GeneratePlayerUI(member.Username));
    }

    public void OnMemberLeft(LobbyMemberData member)
    {
        GameObject.Destroy(userDisplay[member.Username]);
        userDisplay.Remove(member.Username);
    }

    public void OnLobbyLeft()
    {
        foreach(GameObject go in userDisplay.Values)
        {
            Destroy(go);
        }
        FindObjectOfType<RaidUI>().JoinButton.interactable = true;
        gameObject.SetActive(false);
    }

    public void OnChatMessageReceived(LobbyChatPacket packet)
    {
        
    }

    public void OnLobbyPropertyChanged(string property, string value)
    {
        
    }

    public void OnMasterChanged(string masterUsername)
    {
        
    }

    public void OnMemberReadyStatusChanged(LobbyMemberData member, bool isReady)
    {
        
    }

    public void OnMemberTeamChanged(LobbyMemberData member, LobbyTeamData team)
    {
        
    }

    public void OnLobbyStatusTextChanged(string statusText)
    {
        this.statusText.text = statusText;
    }

    public void OnLobbyStateChange(LobbyState state)
    {
        //Automatically start the game when the room is created
        if(state == LobbyState.GameInProgress)
        {
            StartGame();
        }
    }
    #endregion
}
