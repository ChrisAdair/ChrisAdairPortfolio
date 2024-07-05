using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public class StartGamePacket : SerializablePacket
{
    public int roomId;
    public int bedSong;

    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        roomId = reader.ReadInt32();
        bedSong = reader.ReadInt32();
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(roomId);
        writer.Write(bedSong);
    }
}
