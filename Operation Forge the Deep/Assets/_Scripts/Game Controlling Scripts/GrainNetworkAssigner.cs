using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;

public class GrainNetworkAssigner : NetworkBehaviour {

    [Header("Set in Inspector")]
    public TextAsset VertexList;
    public TextAsset TriList;
    public TextAsset GrainVerts;
    public TextAsset NeighborList;
    public GameObject vertPrefab;
    public GameObject orientPrefab;
    public GameObject gbMeshPrefab;
    public GameObject nmsgtSolver;

    [Header("Set Dynamically")]
    public List<Vertex> allVerts;
    public List<Orientation> allGrains;
    public List<GameObject> allFaces;
    public List<int> allTris;
    [SyncVar]
    public bool sgtUpdated;
    public bool sgtRemesh;
    public SyncListFloat boundaries = new SyncListFloat();
    public bool microstructureBuilt = false;
    [SyncVar]
    public float SGTOutput;
    [SyncVar]
    public float ElasticOutput;
    public float currHighScore;
    public float SGTOutProperty
    {
        get
        {
            return SGTOutput;
        }
        set
        {
            SGTOutput = value;
            if(checkHighScore != null && isServer)
            {
                checkHighScore.Invoke();
                if(value > currHighScore)
                {
                    currHighScore = SGTOutput;
                }
            }
        }
    }
    public TextAsset[] structure = null;
    public event Action checkHighScore;


    // Use this for initialization
    void Awake () {
        allVerts = new List<Vertex>();
        allFaces = new List<GameObject>();
        allTris = new List<int>();
        SGTOutput = 0;
    }

    public override void OnStartServer()
    {
        microstructureBuilt = false;

        boundaries.Add(0);
        boundaries.Add(10);
        boundaries.Add(-5);
        boundaries.Add(5);
        boundaries.Add(0);
        boundaries.Add(10);

        if(structure == null || structure.Length<=0)
        {
            //Input all of the GBN data for parsing
            string[] vertexData = VertexList.text.Split('\r');
            string[] triData = TriList.text.Split('\r');
            string[] grainData = GrainVerts.text.Split('\r');
            string[] neighborData = NeighborList.text.Split('\r');
            //Instantiate the vertices from the file
            InstantiateVertices(vertexData);
            //Instantiate the grain neighors and orientation centers
            InstantiateGrain(grainData);
            //Instantiate the GB mesh
            InstantiateGrainBoundaries(neighborData, triData);

            sgtUpdated = false;
            sgtRemesh = true;
        }
        else
        {
#if UNITY_STANDALONE_WIN
            string[] vertexData = structure[0].text.Split('\r');
            string[] orientData = structure[1].text.Split('\r');
            string[] triData = structure[2].text.Split('\r');
            string[] grainData = structure[3].text.Split('\r');
            string[] neighborData = structure[4].text.Split( new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
#else
            string[] vertexData = structure[0].text.Split('\r');
            string[] orientData = structure[1].text.Split('\r');
            string[] triData = structure[2].text.Split('\r');
            string[] grainData = structure[3].text.Split('\r');
            string[] neighborData = structure[4].text.Split( new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
#endif


            //Instantiate the vertices from the file
            InstantiateVertices(vertexData);
            //Instantiate the grain neighors and orientation centers
            InstantiateGrain(grainData, orientData);
            //Instantiate the GB mesh
            InstantiateGrainBoundaries(neighborData, triData);

            sgtUpdated = false;
            sgtRemesh = true;
        }
        GameObject solver = Instantiate(nmsgtSolver);
        NetworkServer.Spawn(solver);
        solver.GetComponent<NMSGTMesherandSolver>().gameController = this;
        solver.GetComponent<ElasticModulusModel>().assigner = this;

        microstructureBuilt = true;
    }


    //Instantiates the vertices from the file
    public void InstantiateVertices(string[] vertexData)
    {
        int numVerts = vertexData.Length;
        Vector3 filePos = Vector3.zero;
        GameObject vertGo;

        //Create the vertices from file
        for (int i = 0; i < numVerts; i++)
        {
            vertGo = Instantiate<GameObject>(vertPrefab);

            Vertex vert = vertGo.GetComponent<Vertex>();
            string[] data = vertexData[i].Split(' ', '\t');
            vertGo.name = i.ToString();
            //Read position from file
            filePos.x = float.Parse(data[0].Trim(), System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint);
            filePos.y = float.Parse(data[1], System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint);
            filePos.z = float.Parse(data[2], System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint);

            
            vert.pos = filePos;
            if(filePos.x == boundaries[0] || filePos.x == boundaries[1])
            {
                vert.xMove = false;
                
            }
            if(filePos.y == boundaries[2] || filePos.y == boundaries[3])
            {
                vert.yMove = false;
            }
            if (filePos.z == boundaries[4] || filePos.z == boundaries[5])
            {
                vert.zMove = false;
            }
            NetworkServer.Spawn(vertGo);
            allVerts.Add(vert);

        }
    }

    //Instantiates the grain neighborhood, with the orientation center as the basket for the connected vertices

    public void InstantiateGrain(string[] grainData)
    {
        int numGrains = grainData.Length;
        GameObject grainCenter;
        for(int i=0; i<numGrains; i++)
        {
            grainCenter = Instantiate<GameObject>(orientPrefab);
            Orientation grain = grainCenter.GetComponent<Orientation>();
            grainCenter.name = "Grain " + i;

            string[] neighbors= grainData[i].Split(' ', '\t');
            int numNeighbors = neighbors.Length;
            Vector3 orientPos = Vector3.zero;
            for(int j=0;j<numNeighbors;j++)
            {
                Vertex tempVert = allVerts[int.Parse(neighbors[j])];
                grain.SetVertex(tempVert.gameObject);
                orientPos += tempVert.pos;
            }
            //Centers the orientation center at the mean position of the connected vertices
            orientPos /= numNeighbors;
            grain.pos = orientPos;
            grain.pos *= (float)numGrains / 10.0f;
            allGrains.Add(grain);

            //Push the creation of the orientation center to the server
            NetworkServer.Spawn(grainCenter);
        }
    }

    public void InstantiateGrain(string[] grainData, string[] orientData)
    {
        int numGrains = grainData.Length;
        GameObject grainCenter;
        for (int i = 0; i < numGrains; i++)
        {
            grainCenter = Instantiate<GameObject>(orientPrefab);
            Orientation grain = grainCenter.GetComponent<Orientation>();
            grainCenter.name = "Grain " + i;

            string[] neighbors = grainData[i].Split(' ', '\t');
            string[] orientations = orientData[i].Split(' ', '\t');
            int numNeighbors = neighbors.Length;
            Vector3 orientPos = Vector3.zero;
            for (int j = 0; j < numNeighbors; j++)
            {
                Vertex tempVert = allVerts[int.Parse(neighbors[j].Trim())];
                grain.SetVertex(tempVert.gameObject);
                orientPos += tempVert.pos;
            }
            //Centers the orientation center at the mean position of the connected vertices
            orientPos /= numNeighbors;
            grain.pos = orientPos;
            grain.pos *= (float)numGrains/10.0f;
            grain.transform.rotation = new Quaternion(float.Parse(orientations[0]), float.Parse(orientations[1]), float.Parse(orientations[2]), float.Parse(orientations[3]));
            allGrains.Add(grain);

            //Push the creation of the orientation center to the server
            NetworkServer.Spawn(grainCenter);
        }
    }
    private void InstantiateGrainBoundaries(string[] inputData)
    {
        for(int i = 0; i<inputData.Length; i++)
        {
            //Split each entry into a set of neighbors
            string[] neighbors = inputData[i].Split(' ','\t');
            for(int j = 0; j < neighbors.Length; j++)
            {
                //Create the boundaries between each pair of neighbors
                //i is the current grain center and neighbors[j] is the current pair opposite
                List<GameObject> faceVerts = allGrains[i].vertices;
                List<GameObject> neighborVerts = allGrains[int.Parse(neighbors[j])].vertices;
                List<GameObject> toUse = new List<GameObject>();
                foreach (GameObject faceV in faceVerts)
                {
                    //removes the vertices from the total vertex set of a grain
                    //to find the bounding face vertices
                    if (neighborVerts.Contains(faceV))
                    {
                        toUse.Add(faceV);
                        
                    }
                }

                string debugOut = "";
                GameObject[] meshInput = new GameObject[faceVerts.Count];
                Vector3[] vertInput = new Vector3[faceVerts.Count];
                for (int k=0; k < toUse.Count; k++)
                {
                    debugOut += toUse[k].name + " ";
                    meshInput[k] = toUse[k];
                    vertInput[k] = toUse[k].transform.position;
                }
                // faceVerts now has the vertices of the face between the two grains
                // Create the triangles from the found vertices

                int numTris = (toUse.Count/3 + toUse.Count%3);
                //Debug.Log("Face will have " + numTris + " triangles");
                //Debug.Log("Current Verts: " + debugOut);


                //TODO create a robust triangulation algorithm
                for(int k=0; k<numTris;k+=1)
                {

                    //Give object references to the orientation centers (needed for misorientation value)

                    GameObject neighbor = allGrains[int.Parse(neighbors[j])].gameObject;                   
                    if(!allGrains[i].neighbors.Contains(neighbor))
                    {
                        allGrains[i].SetNeighbor(neighbor);
                    }


                    //Check if the neighbor has spawned the face already
                    if (!allGrains[int.Parse(neighbors[j])].neighbors.Contains(allGrains[i].gameObject))
                    {
                        //Create a single triangle for the new mesh
                        int[] tri = new int[6];
                        tri[0] = 0;
                        tri[1] = 1;
                        tri[2] = 2;
                        //Add reverse face to bypass culling
                        tri[3] = 3;
                        tri[4] = 5;
                        tri[5] = 4;

                        //This is what sets the current triangle being meshed
                        Vector3[] verts = new Vector3[6];
                        verts[0] = verts[3] = toUse[k].transform.position;
                        verts[1] = verts[4] = toUse[k + 1].transform.position;
                        verts[2] = verts[5] = toUse[k + 2].transform.position;

                        //Record triangle indices for large mesh
                        allTris.Add(int.Parse(toUse[k].name));
                        allTris.Add(int.Parse(toUse[k + 1].name));
                        allTris.Add(int.Parse(toUse[k + 2].name));

                        //Game objects needed for SyncListStruct
                        GameObject[] meshGo = new GameObject[6];
                        meshGo[0] = meshGo[3] = toUse[k];
                        meshGo[1] = meshGo[4] = toUse[k + 1];
                        meshGo[2] = meshGo[5] = toUse[k + 2];
                        //Create a new mesh face from the verts, tris, and vertex game objects
                        GameObject meshFace = Instantiate<GameObject>(gbMeshPrefab);

                        meshFace.GetComponent<GrainMesher>().SetGrainMesh(tri, verts, meshGo);
                        meshFace.GetComponent<GrainMesher>().SetNeighbors(allGrains[i], allGrains[int.Parse(neighbors[j])]);
                        
                        //Give each vertex the reference to the mesh it was just attached to
                        toUse[k].GetComponent<Vertex>().attachedMeshes.Add(meshFace.GetComponent<GrainMesher>());
                        toUse[k + 1].GetComponent<Vertex>().attachedMeshes.Add(meshFace.GetComponent<GrainMesher>());
                        toUse[k + 2].GetComponent<Vertex>().attachedMeshes.Add(meshFace.GetComponent<GrainMesher>());
                        //Spawn the grain boundary on the server
                        allGrains[i].SetFace(meshFace);
                        allGrains[int.Parse(neighbors[j])].SetFace(meshFace);
                        NetworkServer.Spawn(meshFace);
                        allFaces.Add(meshFace);
                    }

                }
            }
            foreach(Vertex vert in allVerts)
            {
                if(vert.attachedMeshes.Count == 0)
                {
                    Destroy(vert.gameObject);
                }
            }
        }
        //Original code for creating a single mesh for all boundaries

        //GameObject meshGo = Instantiate<GameObject>(gbMeshPrefab);

        //mesher = meshGo.GetComponent<GrainMesher>();
        //int[] triangles = ParseTriangles(triData);
        //Vector3[] meshPoints = new Vector3[allVerts.Count];
        //GameObject[] vertGos = new GameObject[allVerts.Count];
        //for (int i = 0; i < allVerts.Count; i++)
        //{
        //    meshPoints[i] = transform.InverseTransformPoint(allVerts[i].pos);
        //    vertGos[i] = allVerts[i].gameObject;
        //}
        //mesher.SetGrainMesh(triangles, meshPoints, vertGos);
        //NetworkServer.Spawn(meshGo);
    }


    private void InstantiateGrainBoundaries(string[] inputData, string[] triangles)
    {
        int[] tris = ParseTriangles(triangles);
        for (int i = 0; i < inputData.Length; i++)
        {
            List<GameObject> faceVerts = allGrains[i].vertices;
            //Add volumetric tri for elastic modulus calculation
            var allGrainVertNames = from vert in faceVerts select int.Parse(vert.name);
            for (int k = 0; k < tris.Length; k += 3)
            {
                if (allGrainVertNames.Contains(tris[k]) && allGrainVertNames.Contains(tris[k + 1]) && allGrainVertNames.Contains(tris[k + 2]))
                {
                    GameObject[] tempTri = new GameObject[] { GameObject.Find((tris[k].ToString())), GameObject.Find((tris[k + 1].ToString())), GameObject.Find((tris[k + 2].ToString())) };
                    allGrains[i].volumetricTris.Add(tempTri);
                }
            }  

            //Split each entry into a set of neighbors
            string[] neighbors = inputData[i].Split(' ', '\t');
            for (int j = 0; j < neighbors.Length; j++)
            {
                //Create the boundaries between each pair of neighbors
                //i is the current grain center and neighbors[j] is the current pair opposite
                
                List<GameObject> neighborVerts = allGrains[int.Parse(neighbors[j])].vertices;
                List<GameObject> toUse = new List<GameObject>();
                foreach (GameObject faceV in faceVerts)
                {
                    //removes the vertices from the total vertex set of a grain
                    //to find the bounding face vertices
                    if (neighborVerts.Contains(faceV))
                    {
                        toUse.Add(faceV);

                    }
                }

                //string debugOut = "";
                GameObject[] meshInput = new GameObject[faceVerts.Count];
                Vector3[] vertInput = new Vector3[faceVerts.Count];
                List<int> vertNames = new List<int>();

                for (int k = 0; k < toUse.Count; k++)
                {
                    //debugOut += toUse[k].name + " ";
                    meshInput[k] = toUse[k];
                    vertInput[k] = toUse[k].transform.position;
                    vertNames.Add(int.Parse(toUse[k].name));
                }
                // faceVerts now has the vertices of the face between the two grains
                // Create the triangles from the found vertices

                for (int k = 0; k < tris.Length; k += 3)
                {
                    
                    //Check if the triangle from the file is in the current vertex set, if not continue
                    bool correctTri = vertNames.Contains(tris[k]) && vertNames.Contains(tris[k + 1]) && vertNames.Contains(tris[k + 2]);
                    if (!correctTri) continue;

                    //Give object references to the orientation centers (needed for misorientation value)

                    GameObject neighbor = allGrains[int.Parse(neighbors[j])].gameObject;
                    if (!allGrains[i].neighbors.Contains(neighbor))
                    {
                        allGrains[i].SetNeighbor(neighbor);
                    }


                    //Check if the neighbor has spawned the face already
                    if (!allGrains[int.Parse(neighbors[j])].neighbors.Contains(allGrains[i].gameObject))
                    {
                        //Create a single triangle for the new mesh
                        int[] tri = new int[6];
                        tri[0] = 0;
                        tri[1] = 1;
                        tri[2] = 2;
                        //Add reverse face to bypass culling
                        tri[3] = 3;
                        tri[4] = 5;
                        tri[5] = 4;

                        //This is what sets the current triangle being meshed
                        Vector3[] verts = new Vector3[6];
                        verts[0] = verts[3] = allVerts[tris[k]].pos;
                        verts[1] = verts[4] = allVerts[tris[k + 1]].pos;
                        verts[2] = verts[5] = allVerts[tris[k + 2]].pos;

                        //Record triangle indices for large mesh
                        allTris.Add(tris[k]);
                        allTris.Add(tris[k+1]);
                        allTris.Add(tris[k+2]);

                        //Game objects needed for SyncListStruct
                        GameObject[] meshGo = new GameObject[6];
                        meshGo[0] = meshGo[3] = allVerts[tris[k]].gameObject;
                        meshGo[1] = meshGo[4] = allVerts[tris[k + 1]].gameObject;
                        meshGo[2] = meshGo[5] = allVerts[tris[k + 2]].gameObject;
                        //Create a new mesh face from the verts, tris, and vertex game objects
                        GameObject meshFace = Instantiate<GameObject>(gbMeshPrefab);

                        meshFace.GetComponent<GrainMesher>().SetGrainMesh(tri, verts, meshGo);
                        meshFace.GetComponent<GrainMesher>().SetNeighbors(allGrains[i], allGrains[int.Parse(neighbors[j])]);

                        //Give each vertex the reference to the mesh it was just attached to
                        allVerts[tris[k]].attachedMeshes.Add(meshFace.GetComponent<GrainMesher>());
                        allVerts[tris[k + 1]].attachedMeshes.Add(meshFace.GetComponent<GrainMesher>());
                        allVerts[tris[k + 2]].attachedMeshes.Add(meshFace.GetComponent<GrainMesher>());
                        //Spawn the grain boundary on the server
                        allGrains[i].SetFace(meshFace);
                        allGrains[int.Parse(neighbors[j])].SetFace(meshFace);
                        NetworkServer.Spawn(meshFace);
                        allFaces.Add(meshFace);
                    }

                }
            }
        }
        foreach (Vertex vert in allVerts)
        {
            if (vert.attachedMeshes.Count == 0)
            {
                //vert.gameObject.SetActive(false);
            }
        }
    }

    public int[] ParseTriangles(string[] triData)
    {
        List<int> triangles = new List<int>();
        

        for(int i=0; i < triData.Length; i++)
        {
            string[] tri = triData[i].Split(' ', '\t');
            triangles.Add(int.Parse(tri[0]));
            triangles.Add(int.Parse(tri[1]));
            triangles.Add(int.Parse(tri[2]));
        }


        return triangles.ToArray();
    }

}
