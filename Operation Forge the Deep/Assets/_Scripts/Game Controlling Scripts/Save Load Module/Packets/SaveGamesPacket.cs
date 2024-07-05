using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public class SaveGamesPacket : SerializablePacket
{

    public int saveID;
    public float score;


    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        saveID = reader.ReadInt32();
        score = reader.ReadSingle();
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(saveID);
        writer.Write(score);
    }
}
