using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public class RaidDataPacket : SerializablePacket
{
    public LoadLevelPacket levelData;
    public LevelPropsPacket levelProps;
    public string description;

    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        levelData = reader.ReadPacket(new LoadLevelPacket());
        levelProps = reader.ReadPacket(new LevelPropsPacket());
        description = reader.ReadString();
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(levelData);
        writer.Write(levelProps);
        writer.Write(description);
    }
}
