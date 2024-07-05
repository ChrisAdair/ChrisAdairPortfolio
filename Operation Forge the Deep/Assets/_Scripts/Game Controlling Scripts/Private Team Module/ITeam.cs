using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public interface ITeam
{
    int ID { get; set; }
    List<ITeamExtension> Members { get; set; }
    int Count { get; }
    ITeamExtension Leader { get; set; }


    void OnLeaveTeam(ITeamExtension player);
    void OnJoinTeam(ITeamExtension player);
    void SetLeader(ITeamExtension newLeader, ITeamExtension oldLeader);
    void OnStartGame(int roomId, ITeamExtension leader);
    void SendReadyCheck();
    void RecieveReadyCheck(ITeamExtension member);
    TeamPacket CreateTeamPacket();
    void SetGameSong(int songId);
}
