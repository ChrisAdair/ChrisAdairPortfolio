using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Barebones.MasterServer;
using Barebones.Networking;
using TMPro;

public class PrivateTeamUI : MonoBehaviour
{
    public Button CreateTeamButton;
    public GameObject Controls;
    public GameObject memberTemplate;
    public GameObject invitationWindow;
    public TextMeshProUGUI invitationText;
    public Button acceptInvitation;
    public Button declineInvitation;
    public Button sendInvitation;
    public TMP_InputField inviteBox;
    public JoinedTeam team;
    public Button leaveTeam;
    public ReadyCheckUI readyCheckUI;
    
    

    // Start is called before the first frame update
    void Start()
    {
        CreateTeamButton.onClick.AddListener(OnMakeTeam);
        Msf.Client.Team.Invited += OnInvited;
        acceptInvitation.onClick.AddListener(AcceptInvitation);
        declineInvitation.onClick.AddListener(DeclineInvitation);
        leaveTeam.onClick.AddListener(OnLeaveTeam);
        sendInvitation.onClick.AddListener(SendInvitation);
        Msf.Client.Team.readyCheck += CheckReadyState;
        Msf.Client.Team.readyStart += StartReadyCheck;

        CheckIfInTeam();
    }

    private void OnMakeTeam()
    {
        Msf.Client.Team.MakeTeam(()=> 
        {
            CreateTeamButton.gameObject.SetActive(false);
            Controls.SetActive(true);
            team = Msf.Client.Team.team;
            team.TeamUpdated += UpdateTeam;
            UpdateTeam();
        });
    }

    private void OnInvited()
    {
        invitationText.text = "Invitation from " + Msf.Client.Team.invitation.inviterUsername + ": \n\rAccept ?";
        invitationWindow.SetActive(true);
    }

    private void AcceptInvitation()
    {
        invitationWindow.SetActive(false);
        Msf.Client.Team.JoinTeam(Msf.Client.Team.invitation.teamId, (joinedTeam) =>
        {
            team = joinedTeam;
            CreateTeamButton.gameObject.SetActive(false);
            Controls.SetActive(true);
            team = Msf.Client.Team.team;
            team.TeamUpdated += UpdateTeam;
            UpdateTeam();
        });
    }

    private void DeclineInvitation()
    {
        invitationWindow.SetActive(false);
    }

    private void SendInvitation()
    {
        Msf.Client.Team.SendInvitation(inviteBox.textComponent.text, () =>
        {

        });
    }
    private void UpdateTeam()
    {
        var players = transform.GetComponentsInChildren<PrivateTeamUIEntry>();
        foreach(PrivateTeamUIEntry player in players)
        {
            Destroy(player.gameObject);
        }

        foreach(string member in team.members)
        {
            GameObject player = Instantiate(memberTemplate, transform);
            var playerData = player.GetComponent<PrivateTeamUIEntry>();
            playerData.username.text = member;
            //Doesn't exist on client
            if(member == team.leader)
            {
                playerData.SetLeader(true);
            }
            else
            {
                playerData.SetLeader(false);
            }
        }
        
    }

    private void CheckIfInTeam()
    {
        Msf.Client.Team.CheckTeam((inTeam) =>
        {
            if (inTeam)
            {
                CreateTeamButton.gameObject.SetActive(false);
                Controls.SetActive(true);
                team = Msf.Client.Team.team;
                team.TeamUpdated += UpdateTeam;
                UpdateTeam();
            }
            else
            {
                team = null;
                CreateTeamButton.gameObject.SetActive(true);
                Controls.SetActive(false);

                var players = transform.GetComponentsInChildren<PrivateTeamUIEntry>();
                foreach (PrivateTeamUIEntry player in players)
                {
                    Destroy(player.gameObject);
                }
            }
        });
    }

    
    private void OnLeaveTeam()
    {
        Msf.Client.Team.LeaveTeam(() =>
        {
            team = null;
            CreateTeamButton.gameObject.SetActive(true);
            Controls.SetActive(false);

            var players = transform.GetComponentsInChildren<PrivateTeamUIEntry>();
            foreach (PrivateTeamUIEntry player in players)
            {
                Destroy(player.gameObject);
            }
        });
        
    }
    
    private void StartReadyCheck(List<bool> readyList)
    {
        readyCheckUI.gameObject.SetActive(true);
        CheckReadyState(readyList);
    }
    private void CheckReadyState(List<bool> readyList)
    {
        readyCheckUI.SetReadyState(readyList, team.leader);
    }
    private void OnDestroy()
    {
        Msf.Client.Team.Invited -= OnInvited;
    }
}
