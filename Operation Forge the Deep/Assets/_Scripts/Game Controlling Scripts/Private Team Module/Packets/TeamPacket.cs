using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public class TeamPacket : SerializablePacket
{
    public List<string> members = new List<string>();
    public string leader = "";
    public int id = 0;


    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        int bytes = reader.ReadInt32();
        members = SerializationExtensions.FromBytes(members, reader.ReadBytes(bytes));
        leader = reader.ReadString();
        id = reader.ReadInt32();
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(SerializationExtensions.ToBytes(members).Length);
        writer.Write(SerializationExtensions.ToBytes(members));
        writer.Write(leader);
        writer.Write(id);
    }
}
