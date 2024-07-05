using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;
using System;

public class PostScorePacket : SerializablePacket
{

    public string username;
    public int levelID;
    public float score;
    public long fileTime; ///File time represented in <see cref="System.DateTime"/>


    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        username = reader.ReadString();
        levelID = reader.ReadInt32();
        score = reader.ReadSingle();
        fileTime = reader.ReadInt64();
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(username);
        writer.Write(levelID);
        writer.Write(score);
        writer.Write(fileTime);
    }
}
