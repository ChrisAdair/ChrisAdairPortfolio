using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public interface ITeamExtension
{
    IPeer Peer { get; set; }
    ITeam Team { get; set; }
    int TeamId { get; set; }

    InvitePacket CreateInvitePacket();
}
