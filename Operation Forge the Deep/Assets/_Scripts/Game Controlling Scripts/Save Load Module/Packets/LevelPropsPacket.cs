using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;
using System;

public class LevelPropsPacket : SerializablePacket
{

    public float CompletionScore;
    public int TimeLimit;
    public int MoveLimit;
    public float TargetScore;
    public CompletionCondition Condition;
    public MechanicCondition Mechanics;
    public List<int> FrozenGrains;

    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        CompletionScore = reader.ReadSingle();
        TimeLimit = reader.ReadInt32();
        MoveLimit = reader.ReadInt32();
        TargetScore = reader.ReadSingle();
        Condition = (CompletionCondition)Enum.Parse(typeof(CompletionCondition), reader.ReadString());
        Mechanics = (MechanicCondition)Enum.Parse(typeof(MechanicCondition), reader.ReadString());
        FrozenGrains = new List<int>();
        int count = reader.ReadInt32();
        for (int i=0;i<count;i++)
        {
            FrozenGrains.Add(reader.ReadInt32());
        }
    }

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(CompletionScore);
        writer.Write(TimeLimit);
        writer.Write(MoveLimit);
        writer.Write(TargetScore);
        writer.Write(Condition.ToString());
        writer.Write(Mechanics.ToString());
        writer.Write(FrozenGrains.Count);
        for(int i=0;i<FrozenGrains.Count;i++)
        {
            writer.Write(FrozenGrains[i]);
        }
    }
}
