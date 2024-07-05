using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public class ReadyUpdatePacket : SerializablePacket
{
    public List<bool> readyList = new List<bool>();

    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        int count = reader.ReadInt32();
        readyList = new List<bool>();
        for(int i = 0; i < count; i++)
        {
            readyList.Add(reader.ReadBoolean());
        }
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(readyList.Count);
        foreach(bool entry in readyList)
        {
            writer.Write(entry);
        }
    }


}
