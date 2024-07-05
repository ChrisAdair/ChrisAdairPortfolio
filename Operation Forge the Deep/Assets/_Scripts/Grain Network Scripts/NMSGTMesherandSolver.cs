using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using g3;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.Providers.LinearAlgebra;
using System.Linq;
using System.IO;


public class NMSGTMesherandSolver : NetworkBehaviour {
    
    [Header("Set in Inspector")]
    public float edgeLength;
    public GameObject debugObject;
    public bool saveStructure = false;
    [Header("Inputs from game")]

    public List<int> gameTris;
    public List<Vertex> gameVerts;
    public List<Vector3> gameNormals;

    [SerializeField]
    public GrainNetworkAssigner gameController;

    [Header("SGT inputs")]

    public List<int> sgtTris;
    public List<Vector3d> sgtVerts;
    public List<RegionRemesher> submeshes;

    public Mesh inputMesh;
    public NTMesh3 sgtMesh;
    private Dictionary<int, List<double>> edgeSaveStructure;
    private int totalVertCount;

    [Header("Debugging outputs")]

    public int edgesRemeshed;
    public float debugDiffusivity;
    float unit = 1000.0f; //Conversion from 10 m box to 1 cm box
    public bool splitFail = false;

    private StreamWriter matlabOutput;
    public override void OnStartServer()
    {
        //Set up writer for MATLAB output file
        if(saveStructure)
            matlabOutput = File.CreateText(@"C:\Users\Adair\Documents\Codename - GRAINS Multiplayer\Assets\matlabStructure.txt");
        edgeSaveStructure = new Dictionary<int, List<double>>();

    }
    public void Start()
    {
        //Attempting to use the Intel MKL in order to speed up the matrix math in the code
        //CREATE the linux pathing for this segment
#if UNITY_STANDALONE_WIN
        
#else
        Debug.Log("Attempting to set the dynamic library for the MKL system");
        Control.NativeProviderPath = @"/usr/lib/";

#endif
        try
        {
            Control.UseNativeMKL();
            Debug.Log(LinearAlgebraControl.Provider);
        }
        catch (System.Exception e)
        {
            Debug.Log("Could not get the MKL library to work: " + e.Message);
            Control.UseManaged();
        }

        Debug.Log(LinearAlgebraControl.Provider);

        //Initialize variables
        sgtTris = new List<int>();
        sgtVerts = new List<Vector3d>();
        submeshes = new List<RegionRemesher>();
        sgtMesh = new NTMesh3(false,false,true);
        //Get the values from the starting game mesh
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GrainNetworkAssigner>();
        if (gameController == null)
        {
            Debug.LogError("Solver could not find the game controller");
        }
        else
        {
            gameTris = gameController.allTris;
            gameVerts = gameController.allVerts;
            sgtTris = gameTris;
            //Add verts to chosen algorithm data structure
            foreach (Vertex vert in gameVerts)
            {
                sgtVerts.Add(vert.pos / unit);
            }

            //Build the non-manifold mesh
            NTMesh3 mesh = new NTMesh3(false, false, true);

            //Add the vertices into the mesh
            for(int i=0; i < sgtVerts.Count; i++)
            {
                mesh.AppendVertex(sgtVerts[i]);
                
            }
            //Add triangles into the mesh
            int triGroup = 0;
            for (int i = 0; i < sgtTris.Count; i += 3)
            {
                mesh.AppendTriangle(sgtTris[i], sgtTris[i + 1], sgtTris[i + 2], triGroup);
                triGroup++;
            }
            //StandardMeshWriter.WriteFile(@"C:\Users\Adair\Documents\Codename - GRAINS Multiplayer\Assets\testInitial.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);

            //Remesh the input for quality
            //mesh = Refine(mesh);
            sgtMesh = mesh;
            //StandardMeshWriter.WriteFile(@"C:\Users\Christopher Adair\Documents\Codename - GRAINS Multiplayer\Assets\testFirstPass.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);

            double Deff = SGTCalculationSplitMethod(mesh);
            //double Deff = SGTCalculation(mesh);
            //Debug.Log("SGT output: " + Deff);
            gameController.SGTOutProperty = (float)Deff;

            
        }
    }


    public void Update()
    {
        if (!gameController.sgtUpdated)
        {
            double Deff;
            if (gameController.sgtRemesh)
            {
                if (!splitFail)
                {
                    UpdateMeshing();
                    Deff = SGTCalculationSplitMethod(sgtMesh);
                }
                else
                {
                    UpdateMeshing();
                    Deff = SGTCalculation(sgtMesh);
                }
            }
            else if(!splitFail)
            {
                Deff = SimpleSGTSplitMethod(sgtMesh);
            }
            else
            {
                Deff = SGTCalculation(sgtMesh);
            }

            //Debug.Log("SGT output: " + Deff);
            gameController.SGTOutProperty = (float)Deff;
            gameController.sgtUpdated = true;
            GetComponent<ElasticModulusModel>()?.UpdateElasticity();
        }
    }


    private void UpdateMeshing()
    {
        sgtVerts.Clear();
        //Add verts to chosen algorithm data structure
        foreach (Vertex vert in gameVerts)
        {
            sgtVerts.Add(vert.pos / unit);
        }



        //Build the non-manifold mesh
        NTMesh3 mesh = new NTMesh3(false, false, true);

        //Add the vertices into the mesh
        for (int i = 0; i < sgtVerts.Count; i++)
        {
            mesh.AppendVertex(sgtVerts[i]);
        }
        //Add triangles into the mesh
        int triGroup = 0;
        for (int i = 0; i < sgtTris.Count; i += 3)
        {
            mesh.AppendTriangle(sgtTris[i], sgtTris[i + 1], sgtTris[i + 2], triGroup);
            triGroup++;
        }

        //StandardMeshWriter.WriteFile(@"C:\Users\Adair\Documents\Codename - GRAINS Multiplayer\Assets\testInitial.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);
        //StandardMeshWriter.WriteFile(@"D:\Work\Codename - GRAINS Multiplayer\Assets\testInitial.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);

        //Remesh the input for quality
        //sgtMesh = Refine(mesh);

        //StandardMeshWriter.WriteFile(@"C:\Users\Adair\Documents\Codename - GRAINS Multiplayer\Assets\testFirstPass.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);
        //StandardMeshWriter.WriteFile(@"C:\Users\Christopher Adair\Documents\Codename - GRAINS Multiplayer\AssetstestFirstPass1.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);
        //mesh = Refine(mesh);
        //StandardMeshWriter.WriteFile(@"D:\Work\Codename - GRAINS Multiplayer\Assets\testSecondPass1.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);
    }

    public NTMesh3 Refine(NTMesh3 mesh)
    {
            int cur_eid = start_edges(mesh);
            bool done = false;
            do
            {
                if (mesh.IsEdge(cur_eid))
                {
                    NTMesh3.EdgeSplitInfo splitInfo;
                    mesh.SplitEdge(cur_eid, out splitInfo);
                }
                cur_eid = next_edge(cur_eid, out done);
            } while (done == false);
        FlipWithQuality(ref mesh);
        return mesh;
    }



    private void FlipWithQuality(ref NTMesh3 meshInput)
    {
        NTMesh3 mesh = new NTMesh3(meshInput);
        foreach (int eID in mesh.EdgeIndices())
        {
            if (mesh.EdgeTrianglesCount(eID)==2)
            {
                Index2i oppositeVerts = mesh.GetEdgeOpposingV(eID);
                if (oppositeVerts.b != NTMesh3.InvalidID)
                {
                    //ADD a check if the edge is the longest in the triangle
                    int[] tris = mesh.EdgeTrianglesItr(eID).ToArray<int>();
                    double edgeLength = Vector3.Magnitude((Vector3)mesh.GetVertex(mesh.GetEdgeV(eID).b) - (Vector3)mesh.GetVertex(mesh.GetEdgeV(eID).a));
                    bool voronoiCondition = true;
                    foreach(int tri in tris)
                    {
                        Index3i triEdges = mesh.GetTriEdges(tri);
                        
                        double e1L = Vector3.Magnitude((Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.a).b)- (Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.a).a));
                        double e2L = Vector3.Magnitude((Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.b).b) - (Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.b).a));
                        double e3L = Vector3.Magnitude((Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.c).b) - (Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.c).a));
                        if (edgeLength < e1L || edgeLength < e2L || edgeLength < e3L)
                        {
                            voronoiCondition = false;
                            break;
                        }    
                    }

                    if ( voronoiCondition)
                    {
                        NTMesh3.EdgeFlipInfo info;
                        double beforeQuality = 0;
                        foreach (int tID in mesh.EdgeTrianglesItr(eID))
                        {
                            Index3i verts = mesh.GetTriangle(tID);
                            Vector3[] input = { (Vector3)mesh.GetVertex(verts[0]) * 100, (Vector3)mesh.GetVertex(verts[1]) * 100, (Vector3)mesh.GetVertex(verts[2]) * 100 };
                            beforeQuality += MeshingAlgorithms.ShapeQuality(input);

                        }
                        //beforeQuality = MeshingAlgorithms.OrthogonalQuality(ref mesh, tris);

                        MeshResult result = mesh.FlipEdge(eID, out info);
                        if (result == MeshResult.Failed_FlippedEdgeExists)
                            Debug.Log("Should fail here: Edge " + eID);

                        double afterQuality = 0;
                        foreach (int tID in mesh.EdgeTrianglesItr(eID))
                        {
                            Index3i verts = mesh.GetTriangle(tID);
                            Vector3[] input = { (Vector3)mesh.GetVertex(verts[0]) * 100, (Vector3)mesh.GetVertex(verts[1]) * 100, (Vector3)mesh.GetVertex(verts[2]) * 100 };
                            afterQuality += MeshingAlgorithms.ShapeQuality(input);
                        }
                        //afterQuality = MeshingAlgorithms.OrthogonalQuality(ref mesh, tris);

                        //Debug.Log("Quality Before: " + beforeQuality + "\n\rQuality After: " + afterQuality);
                        if (afterQuality >= beforeQuality || result == MeshResult.Failed_FlippedEdgeExists)
                        {
                            //Debug.Log("EdgeID: " + info.eID + " did not improve quality");
                            mesh.Copy(meshInput);
                        }
                        else if (afterQuality < beforeQuality)
                        {
                            //Debug.Log("Quality improved: Edge " + info.eID);
                            meshInput.Copy(mesh);
                        }
                    }
                }
            }
        }

        //foreach (int eID in mesh.EdgeIndices())
        //{
        //    if (mesh.EdgeTrianglesCount(eID) == 2)
        //    {
        //        Index2i oppositeVerts = mesh.GetEdgeOpposingV(eID);
        //        if (oppositeVerts.b != NTMesh3.InvalidID)
        //        {
        //            //ADD a check if the edge is the longest in the triangle
        //            int[] tris = mesh.EdgeTrianglesItr(eID).ToArray<int>();
        //            double edgeLength = Vector3.Magnitude((Vector3)mesh.GetVertex(mesh.GetEdgeV(eID).b) - (Vector3)mesh.GetVertex(mesh.GetEdgeV(eID).a));
        //            bool voronoiCondition = true;
        //            foreach (int tri in tris)
        //            {
        //                Index3i triEdges = mesh.GetTriEdges(tri);

        //                double e1L = Vector3.Magnitude((Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.a).b) - (Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.a).a));
        //                double e2L = Vector3.Magnitude((Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.b).b) - (Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.b).a));
        //                double e3L = Vector3.Magnitude((Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.c).b) - (Vector3)mesh.GetVertex(mesh.GetEdgeV(triEdges.c).a));
        //                if (edgeLength < e1L || edgeLength < e2L || edgeLength < e3L)
        //                {
        //                    voronoiCondition = false;
        //                    break;
        //                }
        //            }

        //            //Original condition for flipping
        //            //mesh.GetEdgeV(eID).LengthSquared * 0.9 > (mesh.GetVertex(oppositeVerts.a) - mesh.GetVertex(oppositeVerts.b)).LengthSquared
        //            if (voronoiCondition)
        //            {
        //                NTMesh3.EdgeFlipInfo info;
        //                double beforeQuality = 0;
        //                beforeQuality = MeshingAlgorithms.OrthogonalQuality(ref mesh, tris);

        //                MeshResult result = mesh.FlipEdge(eID, out info);
        //                if (result == MeshResult.Failed_FlippedEdgeExists)
        //                    Debug.Log("Should fail here: Edge " + eID);
        //                //Debug.Log("EdgeID: " + info.eID + " Meshing Result: " + result);

        //                double afterQuality = 0;
        //                afterQuality += MeshingAlgorithms.OrthogonalQuality(ref mesh, tris);

        //                if (afterQuality >= beforeQuality || result == MeshResult.Failed_FlippedEdgeExists)
        //                {

        //                }
        //                else if (afterQuality < beforeQuality)
        //                {
        //                    meshInput.Copy(mesh);
        //                    //StandardMeshWriter.WriteFile(@"C:\Users\Adair\Documents\Codename - GRAINS Multiplayer\Assets\Pass " + eID +".obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);
        //                }
        //            }
        //        }
        //    }
        //}
    }

    


    //Helper functions for Refine(NTMesh3 mesh)
    const int nPrime = 31337;     // any prime will do...
    int nMaxEdgeID;
    protected virtual int start_edges(NTMesh3 mesh)
    {
        nMaxEdgeID = mesh.MaxEdgeID;
        return 0;
    }
    protected virtual int next_edge(int cur_eid, out bool bDone)
    {
        int new_eid = (cur_eid + nPrime) % nMaxEdgeID;
        bDone = (new_eid == 0);
        return new_eid;
    }






    public double SGTCalculation(NTMesh3 mesh)
    {
        double Deff = 0;

        //Get bounding box
        var boundaries = mesh.GetBounds();


        //Remove vertices from calculation that have no interior connections
        int computeVertices = mesh.VertexCount;
        foreach (int vID in mesh.VertexIndices())
        {
            bool hasInteriorEdges = false;
            foreach (int eID in mesh.VtxEdgesItr(vID))
            {
                if (!mesh.IsBoundaryEdge(eID) && !(mesh.EdgeTrianglesCount(eID) > 2))
                {
                    hasInteriorEdges = true;
                    break;
                }
            }
            if (!hasInteriorEdges)
            {
                computeVertices--;
            }
        }

        //Find the source and sink nodes
        List<int> sourceVerts = new List<int>();
        List<int> sinkVerts = new List<int>();

        foreach (int eID in mesh.EdgeIndices())
        {
            if (mesh.IsBoundaryEdge(eID))
            {
                continue;
            }
            Index2i edgeData = mesh.GetEdgeV(eID);
            Vector3d vertA = mesh.GetVertex(edgeData.a);
            Vector3d vertB = mesh.GetVertex(edgeData.b);


            //Enumerate source/sink nodes
            if (!sourceVerts.Contains(edgeData.a) && !sinkVerts.Contains(edgeData.a))
            {
                if (vertA.x == 0)
                    sourceVerts.Add(edgeData.a);
                if (vertA.x == boundaries.Width)
                    sinkVerts.Add(edgeData.a);
            }
            if (!sourceVerts.Contains(edgeData.b) && !sinkVerts.Contains(edgeData.b))
            {
                if (vertB.x == 0)
                    sourceVerts.Add(edgeData.b);
                if (vertB.x == boundaries.Width)
                    sinkVerts.Add(edgeData.b);
            }

        }

        totalVertCount = computeVertices - sourceVerts.Count - sinkVerts.Count + 2;
        

        //Create wieghted Adjacency matrix

        Matrix<double> weightedMatrix = Matrix<double>.Build.Dense(totalVertCount, totalVertCount);
        Dictionary<int, int> vertexMapping = new Dictionary<int, int>(); //Key is DMesh3 vertex index, value is the matrix index

        double widthBoundary = 5 * Mathf.Pow(10, -10); //All boundaries modeled with a width of 5 angstroms
        int vertIdx = 0;

        //Find the difusance, and replace the edge connectivity with the supernodes, and build the adjacency matrix (step 1)
        foreach (int eID in mesh.EdgeIndices())
        {
            //ADD check if the edge is a part of only one grain (not in the boundary)
            if (mesh.IsBoundaryEdge(eID) || mesh.EdgeTrianglesCount(eID)>2)
            {
                continue;
            }
            int vertAidx = -1;
            int vertBidx = -1;
            Index2i edgeVData = mesh.GetEdgeV(eID);
            Vector3d vertA = mesh.GetVertex(edgeVData.a);
            Vector3d vertB = mesh.GetVertex(edgeVData.b);
            int[] edgeTData = mesh.EdgeTrianglesItr(eID).ToArray<int>();
            Vector3d dummy = Vector3d.Zero;
            double dummy2 = 0;
            Vector3d faceA = Vector3d.Zero;
            Vector3d faceB = Vector3d.Zero;
            mesh.GetTriInfo(edgeTData[0], out dummy, out dummy2, out faceA);
            mesh.GetTriInfo(edgeTData[1], out dummy, out dummy2, out faceB);




            //Check if current vertex is a source or a sink, if so give it the supernode index
            //If not, give it the mapped matrix index if it exists, or map a brand new vertex
            bool sourceOrSink = false;
            if (sourceVerts.Contains(edgeVData.a))
            {
                vertAidx = totalVertCount - 2;
                sourceOrSink = true;
            }
            if (sinkVerts.Contains(edgeVData.a))
            {
                vertAidx = totalVertCount - 1;
                sourceOrSink = true;
            }

            if (!sourceOrSink)
            {
                if (vertexMapping.ContainsKey(edgeVData.a))
                {
                    vertAidx = vertexMapping[edgeVData.a];
                }
                else
                {
                    vertAidx = vertIdx;
                    vertexMapping.Add(edgeVData.a, vertIdx);
                    vertIdx++;
                }
            }

            sourceOrSink = false;
            if (sourceVerts.Contains(edgeVData.b))
            {
                vertBidx = totalVertCount - 2;
                sourceOrSink = true;
            }
            if (sinkVerts.Contains(edgeVData.b))
            {
                vertBidx = totalVertCount - 1;
                sourceOrSink = true;
            }
            //If b is not a source or sink node, check if has already been assigned a matrix index
            if (!sourceOrSink)
            {
                if (vertexMapping.ContainsKey(edgeVData.b))
                {
                    vertBidx = vertexMapping[edgeVData.b];
                }
                else
                {
                    vertBidx = vertIdx;
                    vertexMapping.Add(edgeVData.b, vertIdx);
                    vertIdx++;
                }
            }

            //Calculate the edge lengths
            Vector3d diff = vertA - vertB;
            double length = diff.Length;

            //Calculate the grain boundary thickness from the adjacent faces
            double thickness = (faceA - faceB).Length;


            //Compute diffusance of the current edge
            double gbArea = thickness * widthBoundary;
            //ASSUMPTION here is that the edge will always use the diffusivity of the first triangle in the set
            double diffusance = gameController.allFaces[mesh.GetTriangleGroup(edgeTData[0])].GetComponent<GrainMesher>().diffusivity * gbArea / length;

            //Debugging lines showing the source and sink edges in red and the rest in black
            //if (sourceOrSink || sourceVerts.Contains(edgeVData.a) || sinkVerts.Contains(edgeVData.a))
            //    Debug.DrawLine((Vector3)vertA * 1000, (Vector3)vertB * 1000, Color.red, 60.0f, false);
            //else
            //    Debug.DrawLine((Vector3)vertA * 1000, (Vector3)vertB * 1000, Color.black, 60.0f, false);

            weightedMatrix[vertAidx, vertBidx] += diffusance;
            weightedMatrix[vertBidx, vertAidx] += diffusance;


            var outNormal = gameController.allFaces[mesh.GetTriangleGroup(edgeTData[0])].GetComponent<GrainMesher>().normal;
            if(saveStructure)
                matlabOutput.WriteLine(vertAidx + " " + vertBidx + " " + gameController.allFaces[mesh.GetTriangleGroup(edgeTData[0])].GetComponent<GrainMesher>().neighbors + " " 
                    + thickness + " " + length + " " + outNormal.x + " " + outNormal.y + " " + outNormal.z);
            
        }

        var colSums = weightedMatrix.ColumnSums();
        Matrix<double> diagColSums = Matrix<double>.Build.DenseOfDiagonalVector(colSums);
        weightedMatrix = diagColSums - weightedMatrix; //Completed adjacency matrix

        
        //Compute eigendecomposition
        Evd<double> eigen = weightedMatrix.Evd(Symmetricity.Symmetric);

        Vector<double> eigenvalues = eigen.EigenValues.Real();
        var eigenvectors = eigen.EigenVectors;


        //Compute Effective Diffusivity
        for (int i = 1; i < eigenvalues.Count; i++)
        {
            //Debug.Log("Current Eigenvalue index:" + i);
            Deff += (1 / eigenvalues[i]) * System.Math.Pow((eigenvectors.At(totalVertCount - 2, i) - eigenvectors.At(totalVertCount - 1, i)), 2);
        }
        Deff = 1 / Deff;

        Deff = Deff * boundaries.Width / (boundaries.Height * boundaries.Depth);
        //Debug.Log(weightedMatrix.Column(132).ToString());
        return Deff;
    }
    public double SGTCalculationSplitMethod(NTMesh3 mesh)
    {

        double Deff = 0;
        edgeSaveStructure.Clear();

        //Get bounding box
        var boundaries = mesh.GetBounds();


        //Remove vertices from calculation that have no interior connections
        //Also check that the interior connection is not just a triple line
        int computeVertices = mesh.VertexCount;
        foreach (int vID in mesh.VertexIndices())
        {
            bool hasInteriorEdges = false;
            foreach (int eID in mesh.VtxEdgesItr(vID))
            {
                if (!mesh.IsBoundaryEdge(eID) && !(mesh.EdgeTrianglesCount(eID)>2))
                {
                    hasInteriorEdges = true;
                    break;
                }
            }
            if (!hasInteriorEdges)
            {
                computeVertices--;
            }
        }

        //Find the source and sink nodes
        List<int> sourceVerts = new List<int>();
        List<int> sinkVerts = new List<int>();

        foreach (int eID in mesh.EdgeIndices())
        {
            if (mesh.IsBoundaryEdge(eID) || mesh.EdgeTrianglesCount(eID)>2)
            {
                continue;
            }
            Index2i edgeData = mesh.GetEdgeV(eID);
            Vector3d vertA = mesh.GetVertex(edgeData.a);
            Vector3d vertB = mesh.GetVertex(edgeData.b);


            //Enumerate source/sink nodes
            if (!sourceVerts.Contains(edgeData.a) && !sinkVerts.Contains(edgeData.a))
            {
                if (vertA.x == 0)
                    sourceVerts.Add(edgeData.a);
                if (vertA.x == boundaries.Width)
                    sinkVerts.Add(edgeData.a);
            }
            if (!sourceVerts.Contains(edgeData.b) && !sinkVerts.Contains(edgeData.b))
            {
                if (vertB.x == 0)
                    sourceVerts.Add(edgeData.b);
                if (vertB.x == boundaries.Width)
                    sinkVerts.Add(edgeData.b);
            }

        }

        totalVertCount = computeVertices - sourceVerts.Count - sinkVerts.Count + 2;


        //Create wieghted Adjacency matrix

        Matrix<double> weightedMatrix = Matrix<double>.Build.Dense(totalVertCount, totalVertCount);
        Dictionary<int, int> vertexMapping = new Dictionary<int, int>(); //Key is DMesh3 vertex index, value is the matrix index

        double widthBoundary = 5 * Mathf.Pow(10, -10); //All boundaries modeled with a width of 5 angstroms
        int vertIdx = 0;

        //Find the difusance, and replace the edge connectivity with the supernodes, and build the adjacency matrix (step 1)
        foreach (int eID in mesh.EdgeIndices())
        {
            //ADD check if the edge is a part of only one grain (not in the boundary)
            if (mesh.IsBoundaryEdge(eID) || mesh.EdgeTrianglesCount(eID) > 2)
            {
                continue;
            }
            int vertAidx = -1;
            int vertBidx = -1;
            Index2i edgeVData = mesh.GetEdgeV(eID);
            Vector3d vertA = mesh.GetVertex(edgeVData.a);
            Vector3d vertB = mesh.GetVertex(edgeVData.b);
            int[] edgeTData = mesh.EdgeTrianglesItr(eID).ToArray<int>();
            Vector3d dummy = Vector3d.Zero;
            double dummy2 = 0;
            Vector3d faceA = Vector3d.Zero;
            Vector3d faceB = Vector3d.Zero;
            mesh.GetTriInfo(edgeTData[0], out dummy, out dummy2, out faceA);
            mesh.GetTriInfo(edgeTData[1], out dummy, out dummy2, out faceB);




            //Check if current vertex is a source or a sink, if so give it the supernode index
            //If not, give it the mapped matrix index if it exists, or map a brand new vertex
            bool sourceOrSink = false;
            if (sourceVerts.Contains(edgeVData.a))
            {
                vertAidx = totalVertCount - 2;
                sourceOrSink = true;
            }
            if (sinkVerts.Contains(edgeVData.a))
            {
                vertAidx = totalVertCount - 1;
                sourceOrSink = true;
            }

            if (!sourceOrSink)
            {
                if (vertexMapping.ContainsKey(edgeVData.a))
                {
                    vertAidx = vertexMapping[edgeVData.a];
                }
                else
                {
                    vertAidx = vertIdx;
                    vertexMapping.Add(edgeVData.a, vertIdx);
                    vertIdx++;
                }
            }

            sourceOrSink = false;
            if (sourceVerts.Contains(edgeVData.b))
            {
                vertBidx = totalVertCount - 2;
                sourceOrSink = true;
            }
            if (sinkVerts.Contains(edgeVData.b))
            {
                vertBidx = totalVertCount - 1;
                sourceOrSink = true;
            }
            //If b is not a source or sink node, check if has already been assigned a matrix index
            if (!sourceOrSink)
            {
                if (vertexMapping.ContainsKey(edgeVData.b))
                {
                    vertBidx = vertexMapping[edgeVData.b];
                }
                else
                {
                    vertBidx = vertIdx;
                    vertexMapping.Add(edgeVData.b, vertIdx);
                    vertIdx++;
                }
            }

            //Calculate the edge lengths
            Vector3d diff = vertA - vertB;
            double length = diff.Length;

            //Calculate the grain boundary thickness from the adjacent faces
            double thickness = (faceA - faceB).Length;


            //Compute diffusance of the current edge
            double gbArea = thickness * widthBoundary;
            //ASSUMPTION here is that the edge will always use the diffusivity of the first triangle in the set
            double diffusance = gameController.allFaces[mesh.GetTriangleGroup(edgeTData[0])].GetComponent<GrainMesher>().diffusivity * gbArea / length;

            //Debugging lines showing the source and sink edges in red and the rest in black
            //if (sourceOrSink || sourceVerts.Contains(edgeVData.a) || sinkVerts.Contains(edgeVData.a))
            //    Debug.DrawLine((Vector3)vertA * 1000, (Vector3)vertB * 1000, Color.red, 60.0f, false);
            //else
            //    Debug.DrawLine((Vector3)vertA * 1000, (Vector3)vertB * 1000, Color.black, 60.0f, false);
            weightedMatrix[vertAidx, vertBidx] += diffusance;
            weightedMatrix[vertBidx, vertAidx] += diffusance;

            edgeSaveStructure.Add(eID, new List<double>() { vertAidx, vertBidx, gbArea / length });

            
            if (saveStructure)
            {
                var outNormal = gameController.allFaces[mesh.GetTriangleGroup(edgeTData[0])].GetComponent<GrainMesher>().normal;
                matlabOutput.WriteLine(vertAidx + " " + vertBidx + " " + gameController.allFaces[mesh.GetTriangleGroup(edgeTData[0])].GetComponent<GrainMesher>().neighbors + " "
                    + thickness + " " + length + " " + outNormal.x + " " + outNormal.y + " " + outNormal.z);
            }
                

        }
        if(vertexMapping.Count+2 != totalVertCount)
        {
            for(int j = 0; j < totalVertCount - (vertexMapping.Count + 2);j++)
            {
                weightedMatrix = weightedMatrix.RemoveColumn(totalVertCount - 3);
                weightedMatrix = weightedMatrix.RemoveRow(totalVertCount - 3);
            }
            totalVertCount = vertexMapping.Count + 2;
            //need to also change the vertex mapping in the save edges structure
            //Need to find a way to search the values for the source and sink nodes, rather than the keys

        }
        var colSums = weightedMatrix.ColumnSums();
        Matrix<double> diagColSums = Matrix<double>.Build.DenseOfDiagonalVector(colSums);
        weightedMatrix = diagColSums - weightedMatrix; //Completed adjacency matrix
        //Utilize split method instead of eigendecomposition
        Matrix<double> Q = weightedMatrix.Clone();
        //Set the source and sink rows to 1 on their node index
        Vector<double> temp = Vector<double>.Build.Dense(colSums.Count, 0);
        Q.SetRow(Q.RowCount-2,temp); 
        Q.SetRow(Q.RowCount-1, temp);
        Q[Q.RowCount - 2, Q.ColumnCount - 2] = 1;
        Q[Q.RowCount - 1, Q.ColumnCount - 1] = 1;

        temp[Q.RowCount - 2] = 1;
        temp = Q.LU().Solve(temp);
        double massFlow = -weightedMatrix.Row(weightedMatrix.RowCount - 1).DotProduct(temp);

        Deff = boundaries.Width / (boundaries.Height * boundaries.Depth) * massFlow;
        //Debug.Log(Q.Column(392).ToString());
        if (double.IsNaN(Deff))
            splitFail = true;
        return Deff;
    }
    
    public double SimpleSGTSplitMethod(NTMesh3 mesh)
    {

        double Deff = 0;

        //Matrix<double> weightedMatrix = Matrix<double>.Build.Dense(totalVertCount, totalVertCount);
        double[,] wMat = new double[totalVertCount,totalVertCount];
        foreach(int i in edgeSaveStructure.Keys)
        {
            int[] edgeTData = mesh.EdgeTrianglesItr(i).ToArray<int>();
            double diffusance = gameController.allFaces[mesh.GetTriangleGroup(edgeTData[0])].GetComponent<GrainMesher>().diffusivity * edgeSaveStructure[i][2];

            //weightedMatrix[(int)edgeSaveStructure[i][0], (int)edgeSaveStructure[i][1]] += diffusance;
            //weightedMatrix[(int)edgeSaveStructure[i][1], (int)edgeSaveStructure[i][0]] += diffusance;
            wMat[(int)edgeSaveStructure[i][0], (int)edgeSaveStructure[i][1]] += diffusance;
            wMat[(int)edgeSaveStructure[i][1], (int)edgeSaveStructure[i][0]] += diffusance;
        }

        //var colSums = weightedMatrix.ColumnSums();

        double temp = 0;
        for (int i=0;i<totalVertCount;i++)
        {
            for(int j=0;j<totalVertCount;j++)
            {
                temp += wMat[i, j];
                wMat[i, j] = -wMat[i, j];
            }
            wMat[i, i] += temp;
            temp = 0;
        }
        //Matrix<double> diagColSums = Matrix<double>.Build.DenseOfDiagonalVector(colSums);
        //weightedMatrix = diagColSums - weightedMatrix; //Completed adjacency matrix
        
        //Utilize split method instead of eigendecomposition
        //Matrix<double> Q = weightedMatrix.Clone();
        double[,] q = new double[totalVertCount, totalVertCount];
        q = (double[,]) wMat.Clone();
        //Set the source and sink rows to 1 on their node index
        /*Vector<double> temp = Vector<double>.Build.Dense(totalVertCount, 0);
        Q.SetRow(Q.RowCount - 2, temp);
        Q.SetRow(Q.RowCount - 1, temp);
        Q[Q.RowCount - 2, Q.ColumnCount - 2] = 1;
        Q[Q.RowCount - 1, Q.ColumnCount - 1] = 1;
        

        temp[Q.RowCount - 2] = 1;
        temp = Q.LU().Solve(temp);
        double massFlow = -weightedMatrix.Row(weightedMatrix.RowCount - 1).DotProduct(temp);
        */
        for (int i = 0;i< totalVertCount;i++)
        {
            q[i, totalVertCount - 1] = 0;
            q[i, totalVertCount - 2] = 0;
        }
        q[totalVertCount - 1, totalVertCount - 1] = 1;
        q[totalVertCount - 2, totalVertCount - 2] = 1;
        double[] rightSide = new double[totalVertCount];
        rightSide[totalVertCount - 2] = 1;
        int[] inter = new int[totalVertCount];
        int one = 1;
        double[] leftSide = new double[totalVertCount*totalVertCount];
        //put into column major upper triangular matrix
        for(int i=0;i<totalVertCount;i++)
        {
            for(int j=0;j<totalVertCount;j++)
            {
                leftSide[i+j*totalVertCount] = q[j, i];
            }
        }
        //THIS IS A BANDAID
        try
        {
            int info = MatrixNative.dgesv(Layout.Col_Major, ref totalVertCount, ref one, leftSide, ref totalVertCount, inter, rightSide, ref totalVertCount);
            double[] dotIn = new double[totalVertCount];
            for (int i = 0; i < totalVertCount; i++)
            {
                dotIn[i] = wMat[i, totalVertCount - 1];
            }
            int inc = 1;
            var massFlow = -MatrixNative.cblas_ddot(ref totalVertCount, dotIn, ref inc, rightSide, ref inc);


            var boundaries = mesh.GetBounds();
            Deff = boundaries.Width / (boundaries.Height * boundaries.Depth) * massFlow;
        }
        catch(System.Exception e)
        {
            Matrix<double> weightedMatrix = Matrix<double>.Build.Dense(totalVertCount, totalVertCount);
            foreach (int i in edgeSaveStructure.Keys)
            {
                int[] edgeTData = mesh.EdgeTrianglesItr(i).ToArray<int>();
                double diffusance = gameController.allFaces[mesh.GetTriangleGroup(edgeTData[0])].GetComponent<GrainMesher>().diffusivity * edgeSaveStructure[i][2];

                weightedMatrix[(int)edgeSaveStructure[i][0], (int)edgeSaveStructure[i][1]] += diffusance;
                weightedMatrix[(int)edgeSaveStructure[i][1], (int)edgeSaveStructure[i][0]] += diffusance;
            }
            var colSums = weightedMatrix.ColumnSums();
            Matrix<double> diagColSums = Matrix<double>.Build.DenseOfDiagonalVector(colSums);
            weightedMatrix = diagColSums - weightedMatrix; //Completed adjacency matrix

            //Utilize split method instead of eigendecomposition
            Matrix<double> Q = weightedMatrix.Clone();

            //Set the source and sink rows to 1 on their node index
            Vector<double> temp2 = Vector<double>.Build.Dense(totalVertCount, 0);
            Q.SetRow(Q.RowCount - 2, temp2);
            Q.SetRow(Q.RowCount - 1, temp2);
            Q[Q.RowCount - 2, Q.ColumnCount - 2] = 1;
            Q[Q.RowCount - 1, Q.ColumnCount - 1] = 1;


            temp2[Q.RowCount - 2] = 1;
            temp2 = Q.LU().Solve(temp2);
            double massFlow = -weightedMatrix.Row(weightedMatrix.RowCount - 1).DotProduct(temp2);
            var boundaries = mesh.GetBounds();
            Deff = boundaries.Width / (boundaries.Height * boundaries.Depth) * massFlow;
        }

        if (double.IsNaN(Deff))
            splitFail = true;
        return Deff;
    }
    public void OnDestroy()
    {
        if(saveStructure)
            matlabOutput.Close();
    }

}
