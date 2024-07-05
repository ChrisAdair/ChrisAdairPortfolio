using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Vertex : NetworkBehaviour, IUndoable {

    [SyncVar(hook ="OnSelection")]
    public bool selected = false;
    public List<GrainMesher> attachedMeshes;
    [SyncVar]
    public bool xMove = true;
    [SyncVar]
    public bool yMove = true;
    [SyncVar]
    public bool zMove = true;

    private MeshRenderer rend;


    public Vector3 pos
    {
        get { return transform.position; }
        set
        {
            transform.position = value;
            
        }
    }

    public GameObject gameObj
    {
        get
        {
            return gameObject;
        }
    }
    public List<Vector3> prevPos
    {
        get { return _prevPos; }
    }
    public List<Quaternion> prevRot
    {
        get { return _prevRot; }
    }
    [SerializeField]
    private List<Vector3> _prevPos;
    private List<Quaternion> _prevRot;



    public void Awake()
    {
        rend = gameObject.GetComponent<MeshRenderer>();
        attachedMeshes = new List<GrainMesher>();
        _prevPos = new List<Vector3>();
        _prevRot = new List<Quaternion>();

    }

    void OnSelection(bool selected)
    {
        
            if (selected)
            {
            if (hasAuthority)
                rend.material.color = Color.green;
            else
                rend.material.color = Color.red;
            }
            else
            {
                rend.material.color = Color.white;
            }
        
    }
    [Command]
    public void CmdUpdateMeshes()
    {
        foreach(GrainMesher mesh in attachedMeshes)
        {
            mesh.updated = false;
        }
    }

    //Implementation of IUndoable
    public void Undo()
    {
        if (_prevPos.Count > 1)
        {
            transform.position = _prevPos[_prevPos.Count - 2];
            CmdUpdateMeshes();
            _prevPos.RemoveAt(_prevPos.Count - 1);
        }
    }

    public void AddState(Quaternion toAdd)
    {
        Debug.Log("Should not call this method");
    }
    public void AddState(Vector3 toAdd)
    {
        _prevPos.Add(toAdd);
    }

    public void InitializeUndo()
    {
        _prevPos.Clear();
        _prevPos.Add(pos);
    }

    public Vector3 GetLastValue()
    {
        return _prevPos[_prevPos.Count - 1];
    }
}
