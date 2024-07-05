using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;
using Barebones.MasterServer;

public class PrivateTeam : ITeam
{
    public int ID { get; set; }
    public List<ITeamExtension> Members { get; set; }

    private int bedSong;
    public int Count
    {
        get
        {
            return Members.Count;
        }
    }
    public ITeamExtension Leader { get; set; }

    private List<bool> readyList;

    public PrivateTeam(int id, ITeamExtension creator)
    {
        ID = id;
        Members = new List<ITeamExtension>();
        Members.Add(creator);
        Leader = creator;
        readyList = new List<bool>();
        readyList.Add(false);
    }


    public void OnJoinTeam(ITeamExtension player)
    {
        if (!Members.Contains(player))
        {
            Members.Add(player);
            readyList.Add(false);
            var packet = CreateTeamPacket();
            BroadcastMessage(MessageHelper.Create((short)PrivateTeamOpCodes.TeamChanged, packet));
        }
            
    }

    public void OnStartGame(int roomId, ITeamExtension leader)
    {
        var packet = new StartGamePacket()
        {
            roomId = roomId,
            bedSong = bedSong
        };
        var message = MessageHelper.Create((short)PrivateTeamOpCodes.ClientStart, packet);
        BroadcastMessage(message);
    }
    public void OnLeaveTeam(ITeamExtension player)
    {
        if (Members.Contains(player))
        {
            Members.Remove(player);
            var packet = CreateTeamPacket();
            BroadcastMessage(MessageHelper.Create((short)PrivateTeamOpCodes.TeamChanged, packet));
        }
    }

    public void SendReadyCheck()
    {
        

        //reset the ready conditions
        for(int i = 0; i > readyList.Count; i++)
        {
            readyList[i] = false;
        }
        var packet = new ReadyUpdatePacket()
        {
            readyList = readyList
        };
        var message = MessageHelper.Create((short)PrivateTeamOpCodes.ServerReadyStart, packet);
        BroadcastMessage(message);
    }

    public void RecieveReadyCheck(ITeamExtension member)
    {
        int idx = Members.IndexOf(member);
        //Index of -1 means the member was not found in the list
        if(idx != -1)
        {
            readyList[idx] = true;
        }
        SendReadyUpdate();

        //Check if the game is ready to start
        if (!readyList.Contains(false))
        {
            OnStartGame(ID, Leader);
        }

    }

    private void SendReadyUpdate()
    {
        var packet = new ReadyUpdatePacket()
        {
            readyList = readyList
        };
        var message = MessageHelper.Create((short)PrivateTeamOpCodes.ServerReadyUpdate, packet);

        BroadcastMessage(message);
    }
    public void SetLeader(ITeamExtension newLeader, ITeamExtension oldLeader)
    {
        if(Leader == oldLeader && Members.Contains(newLeader))
        {
            Leader = newLeader;
            var packet = CreateTeamPacket();
            BroadcastMessage(MessageHelper.Create((short)PrivateTeamOpCodes.TeamChanged, packet));
        }
    }

    public TeamPacket CreateTeamPacket()
    {
        var packet = new TeamPacket();
        packet.id = ID;
        packet.leader = Leader.Peer.GetExtension<IUserExtension>().Username;
        packet.members = Members.ConvertAll<string>(new System.Converter<ITeamExtension, string>((extension) => { return extension.Peer.GetExtension<IUserExtension>().Username; }));

        return packet;
    }
    public void SetGameSong(int songId)
    {
        bedSong = songId;
    }

    private void BroadcastMessage(IMessage message)
    {
        foreach(ITeamExtension player in Members)
        {
            player.Peer.SendMessage(message);
        }
    }


}
