using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILevelProps
{
    
    float CompletionScore { get; set; }
    int TimeLimit { get; set; }
    int MoveLimit { get; set; }
    float TargetScore { get; set; }
    CompletionCondition Completion { get; set; }
    MechanicCondition Mechanics { get; set; }
    List<int> FrozenGrains { get; set; }
}
