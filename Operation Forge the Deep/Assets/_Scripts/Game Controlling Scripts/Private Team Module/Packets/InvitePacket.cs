using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public class InvitePacket : SerializablePacket
{

    public string inviterUsername { get; set; }
    public int teamId { get; set; }

    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        inviterUsername = reader.ReadString();
        teamId = reader.ReadInt32();
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(inviterUsername);
        writer.Write(teamId);
    }
}
