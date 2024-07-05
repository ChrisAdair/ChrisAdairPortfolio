using System.Collections.Generic;
using Barebones.Networking;

public class LoadLevelPacket : SerializablePacket
{

    public int levelID;
    public string levelName;
    public int grainNumber;
    public string fileName;

    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        levelID = reader.ReadInt32();
        levelName = reader.ReadString();
        grainNumber = reader.ReadInt32();
        fileName = reader.ReadString();
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(levelID);
        writer.Write(levelName);
        writer.Write(grainNumber);
        writer.Write(fileName);
    }
}