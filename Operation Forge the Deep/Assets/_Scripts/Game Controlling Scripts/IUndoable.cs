using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUndoable {

    GameObject gameObj { get; }
    List<Vector3> prevPos { get; }
    List<Quaternion> prevRot { get; }
    void Undo();
    void AddState(Vector3 toAdd);
    void AddState(Quaternion toAdd);
    void InitializeUndo();
}
