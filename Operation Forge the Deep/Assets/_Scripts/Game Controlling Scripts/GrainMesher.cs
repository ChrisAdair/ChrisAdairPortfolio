using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using MathNet.Numerics.LinearAlgebra;

public class GrainMesher : NetworkBehaviour {

    public struct meshVerts
    {
        public Vector3 vertex;
        public Transform vertGo;
        

        public meshVerts(GameObject attachedGo)
        {
            vertGo = attachedGo.transform;
            vertex = vertGo.position;
        }
    }
    public class SyncListMesh: SyncListStruct<meshVerts>
    {

    }

    public struct syncOrientations
    {
        public Quaternion orientation
        {
            get { return objectBase.transform.rotation; }
        }
        public GameObject objectBase;

        public syncOrientations(Orientation input)
        {
            objectBase = input.gameObject;
        }
    }
    public class SyncListOrient : SyncListStruct<syncOrientations>
    {
        public override string ToString()
        {
            string output = "";
            for(int i = 0; i < this.Count; i++)
            {
                output += GetItem(i).objectBase.name + " ";
            }
            return output;
        }
    }


    [Header("Set in Inspector")]
    public Material GBMeshMat;
    

    [Header("Set Dynamically")]
    [SerializeField]
    private Mesh mesh;

    public SyncListInt triangles = new SyncListInt();
    public SyncListMesh verts = new SyncListMesh();
    public SyncListOrient neighbors = new SyncListOrient();
    public Material individualMat;
    public Material highlightMat;

    [SyncVar]
    public double diffusivity;
    [SyncVar]
    public bool updated = true;
    public Vector3 normal
    {
        get
        {
            return mesh.normals[0];
        }
    }


    public void SetNeighbors(Orientation a, Orientation b)
    {
        neighbors.Add(new syncOrientations(a));
        neighbors.Add(new syncOrientations(b));
    }
    public void Awake()
    {
        mesh = new Mesh();
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        mesh.MarkDynamic();
        //Create a new shader material for transparency
        individualMat = new Material(Shader.Find("Standard"));
        individualMat.EnableKeyword("_ALPHABLEND_ON");
        individualMat.CopyPropertiesFromMaterial(GBMeshMat);
        gameObject.GetComponent<MeshRenderer>().material = individualMat;
        
    }
    public void Start()
    {
        Remesh();
        //diffusivity = TempDiffModel(neighbors[0].orientation, neighbors[1].orientation, normal);
    }

    public void Update()
    {
        if (updated) return;

        Remesh();
        updated = true;
    }
    public void UpdateDiffusivity()
    {
        //diffusivity = GBEnergyModel(neighbors[0].orientation, neighbors[1].orientation, normal);
        diffusivity = TempDiffModel(neighbors[0].orientation, neighbors[1].orientation, normal);
        //diffusivity = ReadShockleyModel(neighbors[0].orientation, neighbors[1].orientation);
    }
    public void SetGrainMesh(int[] triData, Vector3[] meshPoints, GameObject[] correspondingGo)
    {
        mesh.Clear();
        mesh.vertices = meshPoints;
        for(int i=0;i<meshPoints.Length;i++)
        {
            verts.Add(new meshVerts(correspondingGo[i]));
        }
        mesh.triangles = triData;
        for(int i=0; i<triData.Length; i++)
        {
            triangles.Add(triData[i]);
        }
        mesh.RecalculateNormals();
    }

    public void ClearAndRemesh()
    {
        mesh.Clear();
        Vector3[] temp = new Vector3[verts.Count];
        int idx = 0;
        foreach(meshVerts vertDat in verts)
        {
            temp[idx] = vertDat.vertex;
            idx++;
            
        }
        mesh.vertices = temp;
        int[] triTemp = new int[triangles.Count];
        for(int i=0; i< triangles.Count; i++)
        {
            triTemp[i] = triangles[i];
        }
        mesh.triangles = triTemp;
        mesh.RecalculateNormals();
    }


    public void Remesh()
    {

        //Update mesh location and diffusivity
        Vector3[] temp = new Vector3[verts.Count];
        int idx = 0;
        foreach (meshVerts vertDat in verts)
        {
            temp[idx] = vertDat.vertGo.position;
            idx++;

        }
        mesh.vertices = temp;
        mesh.RecalculateNormals();
        
        UpdateDiffusivity();

        //TODO: Have mesh collider check for collision here

        
        //TODO: Have volumetric checking here for flat grains



    }
    public override void OnStartClient()
    {
        ClearAndRemesh();
    }

    public double TempDiffModel(Quaternion nA, Quaternion nB, Vector3 normal)
    {
        double output = 0;
        Quaternion misorientation = Quaternion.Inverse(nA) * nB;
        misorientation = DisorientationFunctions.Disorientation('c', misorientation);
        float angle = 2*Mathf.Acos(misorientation.w)*Mathf.Rad2Deg;
        normal = normal.normalized;
        output = System.Math.Abs(System.Math.Pow(10, 7) *(angle/10 + System.Math.Abs(normal.x) + System.Math.Abs(normal.y) + System.Math.Abs(normal.z)) +1);
        //output = System.Math.Pow(10, 7);
        //Normalized by the maximum value
        
        return output;
    }

    public double GBEnergyModel(Quaternion nA, Quaternion nB, Vector3 normal)
    {
        double output = 0;

        //Rotate normal so it is in one of the grain frames
        normal = nA * normal;
        //check for mirror condition on the normal
        if (normal.z < 0)
            normal *= -1;
        Vector<double> N = Vector<double>.Build.Dense(new double[] { normal.x, normal.y, normal.z });

        output = GBEnergy.GB5DOF(Quaternion.Inverse(nA) *nB, N, "Ni");

        return output;
    }

    /// <summary>
    /// This version of the Read Shockley model comes from Elsey et all, 2013
    /// </summary>
    /// <param name="nA"></param>
    /// <param name="nB"></param>
    /// <returns>Returns the surface energy of the boundary</returns>
    public double ReadShockleyModel(Quaternion nA, Quaternion nB)
    {
        //If the angle is not below the threshold, return an energy of 1
        double output = 1;

        Quaternion misorientation = Quaternion.Inverse(nA) * nB;
        misorientation = DisorientationFunctions.Disorientation('c', misorientation);
        Vector3 garb;
        float angle;
        misorientation.ToAngleAxis(out angle, out garb);

        if (angle < 30 && angle >0)
        {
            output = 0.1 + 0.9 * angle / 30 * (1 - System.Math.Log(angle / 30));
        }
        else if(angle == 0)
        {
            output = 0.1;
        }
        return output;
    }


    [TargetRpc]
    public void TargetHighlightFace(NetworkConnection toClient)
    {
        gameObject.GetComponent<MeshRenderer>().material = highlightMat;
    }
    [TargetRpc]
    public void TargetUnHighlightFace(NetworkConnection toClient)
    {
        gameObject.GetComponent<MeshRenderer>().material = individualMat;
    }
}
