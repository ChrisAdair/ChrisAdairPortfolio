using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;
using System;

public class LeaderboardPacket : SerializablePacket
{
    public Dictionary<string, float> userValues = new Dictionary<string, float>();
    public LeaderboardType leadType;


    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        int dictLength = reader.ReadInt32();
        userValues = SerializationExtensions.FromBytes(userValues, reader.ReadBytes(dictLength));
        leadType = (LeaderboardType)Enum.Parse(typeof(LeaderboardType), reader.ReadString());
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        var dictBytes = SerializationExtensions.ToBytes(userValues);
        writer.Write(dictBytes.Length);
        writer.Write(dictBytes);
        writer.Write(leadType.ToString());
    }
}
