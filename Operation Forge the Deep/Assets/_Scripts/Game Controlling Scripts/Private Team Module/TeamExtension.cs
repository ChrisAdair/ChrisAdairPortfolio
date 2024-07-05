using System.Collections;
using System.Collections.Generic;
using Barebones.Networking;
using UnityEngine;
using Barebones.MasterServer;

public class TeamExtension : ITeamExtension
{
    public IPeer Peer { get; set; }
    public ITeam Team { get; set; }
    public int TeamId { get; set; }

    public TeamExtension(IPeer peer)
    {
        Peer = peer;
    }
    public InvitePacket CreateInvitePacket()
    {
        var packet = new InvitePacket()
        {
            inviterUsername = Peer.GetExtension<IUserExtension>().Username,
            teamId = TeamId
        };

        return packet;
    }
}
