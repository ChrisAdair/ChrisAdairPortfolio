using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System;
using Barebones.MasterServer;


public class Orientation : NetworkBehaviour , IUndoable
{


    [Header("Set in Inspector")]
    public GameObject grainConnector;
    public Gradient gradient; //Gradient used for coloring the connections
    public Gradient emissionGradient; //Gradient used for connection emission

    [Header("Set Dynamically")]
    //An orientation center will have a list of the vertices 
    //that make up the grain it is a part of

    public List<GameObject> neighbors = new List<GameObject>();
    public List<GameObject> vertices = new List<GameObject>();
    public List<GameObject> faces = new List<GameObject>();
    public List<GrainConnection> connections = new List<GrainConnection>();
    public List<GameObject> connFaces = new List<GameObject>(); //Representative face for each grain boundary, assuming planar boundaries
    public List<GameObject[]> volumetricTris = new List<GameObject[]>();

    
    public bool hasPlayer
    {
        get { return _hasPlayer; }
        set
        {
            if (isServer)
            {
                _hasPlayer = value;
                RpcChangePlayer(_hasPlayer);
            }
        }
    }
    [SyncVar]
    private bool _hasPlayer;
    public bool highlightFaces = false;

    public Vector3 pos
    {
        get { return transform.position; }
        set
        {
            transform.position = value;
        }
    }
    public Quaternion orientation
    {
        get { return transform.rotation; }
    }
    public double totDiffusivity
    {
        get
        {
            double totDiff = 0;
            foreach(GameObject go in faces)
            {
                totDiff += go.GetComponent<GrainMesher>().diffusivity;
            }

            return totDiff;
        }
    }

    //Implementation of IUndoable
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
    private List<Vector3> _prevPos;
    [SerializeField]
    private List<Quaternion> _prevRot;


    public double GrainVolume
    {
        get
        {
            double sum = 0;
            foreach(GameObject[] tri in volumetricTris)
            {
                sum += Math.Abs(Vector3.Dot(-Vector3.Cross(tri[1].transform.position - pos, tri[2].transform.position - pos), tri[0].transform.position - pos)) / 6.0;
            }
            //scale according to actual units
            //actual cube represents a 1 cm^3 material while in game is 1000 m^3
            return sum/1e9;
        }
    }

    public void Awake()
    {
        _prevPos = new List<Vector3>();
        _prevRot = new List<Quaternion>();

    }
    public void Initialize(UnetMsfPlayer player)
    {
        //Get a list of representative faces for the connection objects
        foreach(GameObject neighbor in neighbors)
        {
            Orientation opp = neighbor.GetComponent<Orientation>();
            var face = faces.Intersect(opp.faces).First();
            connFaces.Add(face);
        }

        //Create the connection objects
        TargetSendNeighborData(player.Connection, neighbors.ToArray());
        TargetSendMeshData(player.Connection, connFaces.ToArray());
        
        UpdateDiffusivities();

        TargetCreateConnections(player.Connection);
    }
    public void Initialize(NetworkConnection conn)
    {
        //Get a list of representative faces for the connection objects
        foreach (GameObject neighbor in neighbors)
        {
            Orientation opp = neighbor.GetComponent<Orientation>();
            var face = faces.Intersect(opp.faces).First();
            connFaces.Add(face);
        }

        //Create the connection objects
        TargetSendNeighborData(conn, neighbors.ToArray());
        TargetSendMeshData(conn, connFaces.ToArray());

        UpdateDiffusivities();

        TargetCreateConnections(conn);
    }
    private void Update()
    {
        if (!isClient)
            return;

        if (transform.hasChanged)
        {
            UpdateRepDiffisivities();
            ChangeConnectionColor();
            transform.hasChanged = false;
        }
    }

    public void CreateCylinders(GameObject[] connFaces)
    {
        int faceIdx = 0;
        foreach(GameObject go in neighbors)
        {
            Vector3 direction = go.transform.position - transform.position;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);

            GameObject Cyl = Instantiate(grainConnector, transform.position, rotation);

            Cyl.transform.Translate(0, Cyl.transform.localScale.y, 0);
            var conn = Cyl.GetComponent<GrainConnection>();

            connections.Add(conn);
            conn.origin = transform.position;
            conn.pair = new Orientation[] 
            {
                connFaces[faceIdx].GetComponent<GrainMesher>().neighbors[0].objectBase.GetComponent<Orientation>(),
                connFaces[faceIdx].GetComponent<GrainMesher>().neighbors[1].objectBase.GetComponent<Orientation>()
            };
            conn.face = connFaces[faceIdx].GetComponent<GrainMesher>();

            //CHANGE: Set the difusivity model for the connection here
            conn.diffModel = connFaces[faceIdx].GetComponent<GrainMesher>().TempDiffModel;

            faceIdx++;
        }
    }

    [Client]
    public void ChangeConnectionColor()
    {
        GameObject[] conn = GameObject.FindGameObjectsWithTag("GrainConnection");
        List<Color> colors = new List<Color>();
        List<Color> emission = new List<Color>();

        foreach (GameObject go in conn)
        {
            GrainConnection mesh = go.GetComponent<GrainConnection>();

            //This needs to reflect the current puzzle
            //Temp Puzzle
            double outputNormalized = mesh.face.diffusivity / mesh.maxDiffusivity;
            //Read-Shockley
            //double outputNormalized = mesh.face.diffusivity;

            Color c = Color.white;
            c = gradient.Evaluate((float)outputNormalized);
            Color e = Color.white;
            e = emissionGradient.Evaluate((float)outputNormalized);
            colors.Add(new Color(c.r, c.g, c.b, c.a));
            emission.Add(new Color(e.r, e.g, e.b, e.a));

        }

        for (int i = 0; i < conn.Length; i++)
        {
            conn[i].GetComponent<MeshRenderer>().material.color = colors[i];
            conn[i].GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", emission[i]);
        }
    }

    //If the user is in grain switching mode, highlight the grain they will be switching to
    private void OnMouseEnter()
    {
        if (highlightFaces)
        {
            PlayerBehavior player = NetworkManager.singleton.client.connection.playerControllers[0].gameObject.GetComponent<PlayerBehavior>();
            player.CmdHighlightFace(true, gameObject);
        }
    }
    //private void OnMouseExit()
    //{
    //    if (!isClient)
    //        return;
    //    PlayerBehavior player = NetworkManager.singleton.client.connection.playerControllers[0].gameObject.GetComponent<PlayerBehavior>();
    //    player.CmdHighlightFace(false, gameObject);

    //}

    public void SetVertex(GameObject vert)
    {

        vertices.Add(vert);
    }
    public void SetNeighbor(GameObject neighbor)
    {
        neighbors.Add(neighbor);
    }
    public void SetFace(GameObject face)
    {
        faces.Add(face);
    }
    public List<Vertex> GetVertexList()
    {
        List<Vertex> temp = new List<Vertex>();
        foreach (GameObject idx in vertices)
        {
            temp.Add(idx.GetComponent<Vertex>());
        }
        return temp;
    }

    [ClientRpc]
    private void RpcChangePlayer(bool state)
    {
        _hasPlayer = state;
    }


    [Command]
    public void CmdUpdateDiffusivities()
    {

        UpdateDiffusivities();
    }

    [Client]
    public void UpdateRepDiffisivities()
    {

        foreach (GrainConnection conn in connections)
        {
            conn.ChangeSize();
        }
        foreach (GameObject go in neighbors)
        {
            foreach (GrainConnection conn in go.GetComponent<Orientation>().connections)
            {
                conn.ChangeSize();
            }
        }
    }
    [Server]
    public void UpdateDiffusivities()
    {
        foreach (GameObject go in faces)
        {
            go.GetComponent<GrainMesher>().UpdateDiffusivity();
        }

    }

    [TargetRpc]
    public void TargetSendNeighborData(NetworkConnection conn, GameObject[] neighborServer)
    {
        neighbors = new List<GameObject>(neighborServer);
    }
    [TargetRpc]
    public void TargetSendMeshData(NetworkConnection conn, GameObject[] repMesh)
    {
        connFaces = new List<GameObject>(repMesh);
    }
    [TargetRpc]
    public void TargetCreateConnections(NetworkConnection conn)
    {
        CreateCylinders(connFaces.ToArray());
        ChangeConnectionColor();
    }
    #region IUndoable
    //Implementation of IUndoable
    public void Undo()
    {
        if(_prevRot.Count > 1)
        {
            transform.rotation = _prevRot[_prevRot.Count - 2];
            CmdUpdateDiffusivities();
            _prevRot.RemoveAt(_prevRot.Count - 1);
        }
    }

    public void AddState(Quaternion toAdd)
    {
            _prevRot.Add(toAdd);
    }
    public void AddState(Vector3 toAdd)
    {
        Debug.Log("Should not call this method");
    }

    public void InitializeUndo()
    {
        _prevRot.Clear();
        _prevRot.Add(orientation);
    }

    public Quaternion GetLastValue()
    {
        return _prevRot[_prevRot.Count - 1];
    }
}
#endregion
