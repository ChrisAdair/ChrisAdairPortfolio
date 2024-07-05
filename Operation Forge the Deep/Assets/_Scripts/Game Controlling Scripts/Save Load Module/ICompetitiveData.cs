using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICompetitiveData
{
    int LevelID { get; set; }

    string LevelName { get; set; }

    int GrainNumber { get; set; }

    string FileName { get; set; }
    float CompletionScore { get; set; }
    int TimeLimit { get; set; }
    int MoveLimit { get; set; }
    float TargetScore { get; set; }
    CompletionCondition Completion { get; set; }
    string Description { get; set; }
    MechanicCondition Mechanics { get; set; }
    List<int> FrozenGrains { get; set; }
}
