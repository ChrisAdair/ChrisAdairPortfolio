using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using g3;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

public class SGTMesherAndSolver : NetworkBehaviour {

    [Header("Set in Inspector")]
    public float edgeLength;
    public GameObject debugObject;
    [Header("Inputs from game")]

    public List<int> gameTris;
    public List<Vertex> gameVerts;
    public List<Vector3> gameNormals;

    private GrainNetworkAssigner gameController;

    [Header("SGT inputs")]

    public List<int> sgtTris;
    public List<Vector3d> sgtVerts;
    public List<RegionRemesher> submeshes;

    public Mesh inputMesh;


    [Header("Debugging outputs")]

    public int edgesRemeshed;
    public float debugDiffusivity;
    float unit = 1000; //Conversion from 10 m box to 1 cm box


    public void Start()
    {
        //Initialize variables
        sgtTris = new List<int>();
        sgtVerts = new List<Vector3d>();
        submeshes = new List<RegionRemesher>();

        //Get the values from the starting game mesh
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GrainNetworkAssigner>();
        if(gameController == null)
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
                sgtVerts.Add(vert.pos/unit);
            }


            DMesh3 mesh = DMesh3Builder.Build<Vector3d, int, Vector2d>(sgtVerts, sgtTris,null,new List<int> { 0,1});
            Remesher remesher = new Remesher(mesh);
            remesher.Precompute();
            remesher.EnableSmoothing = false;
            remesher.EnableCollapses = false;
            remesher.SetTargetEdgeLength(mesh.CachedBounds.Depth / 12);
            remesher.BasicRemeshPass();
            //Now check for flips for shorter edges
            FlipWithQuality(ref mesh);
            remesher.EnableFlips = false;
            remesher.BasicRemeshPass();
            FlipWithQuality(ref mesh);
            
            StandardMeshWriter.WriteFile(@"C:\Users\Adair\Documents\Codename - GRAINS Multiplayer\Assets\testFinal.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);


            double Deff = SGTCalculation(mesh);
            Debug.Log("SGT output: " + Deff);
            gameController.SGTOutput = (float)Deff;
        }
    }


    public void Update()
    {
        if (!gameController.sgtUpdated)
        {
            UpdateMeshing();
            gameController.sgtUpdated = true;
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


        DMesh3 mesh = DMesh3Builder.Build<Vector3d, int, Vector2d>(sgtVerts, sgtTris, null, new List<int> { 0, 1 });
        int idx = 0;
        foreach (int tID in mesh.TriangleIndices())
        {
            mesh.SetTriangleGroup(tID, idx);
            idx++;
        }
        Remesher remesher = new Remesher(mesh);
        remesher.EnableCollapses = false;
        remesher.Precompute();
        remesher.EnableSmoothing = false;
        remesher.SetTargetEdgeLength(mesh.CachedBounds.Depth / 12);
        remesher.BasicRemeshPass();
        StandardMeshWriter.WriteFile(@"C:\Users\Adair\Documents\Codename - GRAINS Multiplayer\Assets\testInitial.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);
        //Now check for flips for shorter edges
        FlipWithQuality(ref mesh);
        remesher.EnableFlips = false;
        remesher.BasicRemeshPass();
        FlipWithQuality(ref mesh);
        StandardMeshWriter.WriteFile(@"C:\Users\Adair\Documents\Codename - GRAINS Multiplayer\Assets\testFinal.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);


        double Deff = SGTCalculation(mesh);
        Debug.Log("SGT output: " + Deff);
        gameController.SGTOutput = (float)Deff;
    }
    private void FlipWithQuality(ref DMesh3 meshInput)
    {
        DMesh3 mesh = new DMesh3(meshInput);
        foreach (int eID in mesh.EdgeIndices())
        {
            if (!mesh.IsBoundaryEdge(eID))
            {
                Index2i oppositeVerts = mesh.GetEdgeOpposingV(eID);
                if (oppositeVerts.b != DMesh3.InvalidID)
                {
                    if (mesh.GetEdge(eID).LengthSquared * 0.9 > (mesh.GetVertex(oppositeVerts.a) - mesh.GetVertex(oppositeVerts.b)).LengthSquared)
                    {
                        DMesh3.EdgeFlipInfo info;
                        double beforeQuality = 0;
                        foreach (int tID in mesh.TriangleIndices())
                        {
                            Index3i verts = mesh.GetTriangle(tID);
                            Vector3[] input = { (Vector3)mesh.GetVertex(verts[0])*100, (Vector3)mesh.GetVertex(verts[1])*100, (Vector3)mesh.GetVertex(verts[2])*100 };
                            beforeQuality += MeshingAlgorithms.ShapeQuality(input);
                            
                        }
                        beforeQuality /= mesh.TriangleCount;
                        mesh.FlipEdge(eID, out info);
                        //Debug.Log("EdgeID: " + info.eID + " Meshing Result: " + result);
                        double afterQuality = 0;
                        foreach (int tID in mesh.TriangleIndices())
                        {
                            Index3i verts = mesh.GetTriangle(tID);
                            Vector3[] input = { (Vector3)mesh.GetVertex(verts[0])*100, (Vector3)mesh.GetVertex(verts[1])*100, (Vector3)mesh.GetVertex(verts[2])*100 };
                            afterQuality += MeshingAlgorithms.ShapeQuality(input);
                        }
                        afterQuality /= mesh.TriangleCount;
                        //Debug.Log("Quality Before: " + beforeQuality + "\n\rQuality After: " + afterQuality);
                        if (afterQuality < beforeQuality)
                        {
                            meshInput.Copy(mesh);
                        }
                        else
                        {
                            //Debug.Log("EdgeID: " + info.eID + " did not improve quality");
                            mesh.Copy(meshInput);
                        }
                    }
                }
            }
        }
    }


    public Dictionary<double,double> GetEdgeDiffusivities(DMesh3 mesh)
    {
        Dictionary<double, double> edgeDiff = new Dictionary<double, double>();




        return edgeDiff;
    }
    public double SGTCalculation(DMesh3 mesh, Dictionary<double,double> diffusivities)
    {
        double Deff = 0;

        //float units = 0.01f; //converting from unity units to actual units

        //Structure for vertex [x,y,z]
        //var vertices = mesh.Vertices();
        //Structure for edges [v0,v1,t0,t1] vertices and triangles
        //var edges = mesh.Edges();

        //Get bounding box
        var boundaries = mesh.GetBounds();


        //Find the face centers
        List<Vector3d> faceCenters = new List<Vector3d>();
        foreach(int tID in mesh.TriangleIndices())
        {
            var triVerts = mesh.GetTriangle(tID);
            Vector3d temp = (mesh.GetVertex(triVerts[0]) +
                            mesh.GetVertex(triVerts[1]) +
                            mesh.GetVertex(triVerts[2])) / 3;
            faceCenters.Add(temp);
        }

        //Find the source and sink nodes
        List<int> sourceVerts = new List<int>();
        List<int> sinkVerts = new List<int>();


        foreach (int eID in mesh.EdgeIndices())
        {
            Index4i edgeData = mesh.GetEdge(eID);
            Vector3d vertA = mesh.GetVertex(edgeData.a);
            Vector3d vertB = mesh.GetVertex(edgeData.b);
            //Enumerate source/sink nodes
            if (vertA.x == 0)
                sourceVerts.Add(edgeData.a);
            if (vertA.x == boundaries.Width)
                sinkVerts.Add(edgeData.a);
            if (vertB.x == 0)
                sourceVerts.Add(edgeData.b);
            if (vertB.x == boundaries.Width)
                sinkVerts.Add(edgeData.b);
        }

        //Create wieghted Adjacency matrix
        int totalVertCount = mesh.VertexCount - sourceVerts.Count - sinkVerts.Count + 2;
        Matrix<double> weightedMatrix = Matrix<double>.Build.Dense(totalVertCount, totalVertCount);
        Dictionary<int, int> vertexMapping = new Dictionary<int, int>(); //Key is DMesh3 vertex index, value is the matrix index

        List<double> edgeDiffusance = new List<double>();
        //List<Index2i> edgeConnectivity = new List<Index2i>();
        double widthBoundary = 5 * Mathf.Pow(10, -10); //All boundaries modeled with a width of 5 angstroms
        int vertIdx = 0;

        //Find the difusance, and replace the edge connectivity with the supernodes, and build the adjacency matrix (step 1)
        foreach (int eID in mesh.EdgeIndices())
        {
            int vertAidx = -1;
            int vertBidx = -1;
            Index4i edgeData = mesh.GetEdge(eID);
            Vector3d vertA = mesh.GetVertex(edgeData.a);
            Vector3d vertB = mesh.GetVertex(edgeData.b);

            //Check if current vertex is a source or a sink, if so give it the supernode index
            //If not, give it the mapped matrix index if it exists, or map a brand new vertex
            bool sourceOrSink = false;
            if (sourceVerts.Contains(edgeData.a))
            {
                vertAidx = totalVertCount - 1;
                sourceOrSink = true;
            }
            if (sinkVerts.Contains(edgeData.a))
            {
                vertAidx = totalVertCount;
                sourceOrSink = true;
            }
            if (!sourceOrSink)
            {
                if (vertexMapping.ContainsKey(edgeData.a))
                {
                    vertAidx = vertexMapping[edgeData.a];
                }
                else
                {
                    vertAidx = vertIdx;
                    vertexMapping.Add(edgeData.a, vertIdx);
                    vertIdx++;
                }   
            }

            sourceOrSink = false;
            if (sourceVerts.Contains(edgeData.b))
            {
                vertBidx = totalVertCount - 1;
                sourceOrSink = true;
            }
            if (sinkVerts.Contains(edgeData.b))
            {
                vertBidx = totalVertCount;
                sourceOrSink = true;
            }
            if (!sourceOrSink)
            {
                if (vertexMapping.ContainsKey(edgeData.b))
                {
                    vertBidx = vertexMapping[edgeData.b];
                }
                else
                {
                    vertBidx = vertIdx;
                    vertexMapping.Add(edgeData.b, vertIdx);
                    vertIdx++;
                }
            }
            //Calculate the edge lengths
            Vector3d diff = vertA - vertB;
            double length = diff.Length;

            //Calculate the grain boundary thickness from the adjacent faces
            var faceA = mesh.GetTriCentroid(edgeData.c);
            var faceB = mesh.GetTriCentroid(edgeData.d);

            double thickness = (faceA - faceB).Length;

            //Compute diffusance of the current edge
            double gbArea = thickness * widthBoundary;
            double diffusance = diffusivities[eID] * gbArea / length;
            edgeDiffusance.Add(diffusance);

            weightedMatrix[vertAidx, vertBidx] = diffusance;
            weightedMatrix[vertBidx, vertAidx] = diffusance;
        }

        var colSums = weightedMatrix.ColumnSums();
        Matrix<double> diagColSums = Matrix<double>.Build.DenseOfDiagonalVector(colSums);
        weightedMatrix = diagColSums - weightedMatrix; //Completed adjacency matrix

        //Compute eigendecomposition
        Evd<double> eigen = weightedMatrix.Evd(Symmetricity.Symmetric);

        Vector<double> eigenvalues = eigen.EigenValues.Real();
        var eigenvectors = eigen.EigenVectors;
        

        //TODO: Compute Effective Diffusivity
        for(int i = 1; i < eigenvalues.Count; i++)
        {
            Deff += (1 / eigenvalues[i]) * (eigenvectors[totalVertCount - 1, i] - eigenvectors[totalVertCount, i]);
        }
        Deff = 1 / Deff;

        Deff = Deff * boundaries.Width / (boundaries.Height * boundaries.Depth);

        return Deff;
    }

    public double SGTCalculation(DMesh3 mesh)
    {
        double Deff = 0;

        //float units = 0.01f; //converting from unity units to actual units

        //Structure for vertex [x,y,z]
        //var vertices = mesh.Vertices();
        //Structure for edges [v0,v1,t0,t1] vertices and triangles
        //var edges = mesh.Edges();

        //Get bounding box
        var boundaries = mesh.GetBounds();


        //Find the face centers
        List<Vector3d> faceCenters = new List<Vector3d>();
        foreach (int tID in mesh.TriangleIndices())
        {
            var triVerts = mesh.GetTriangle(tID);
            Vector3d temp = (mesh.GetVertex(triVerts[0]) +
                            mesh.GetVertex(triVerts[1]) +
                            mesh.GetVertex(triVerts[2])) / 3;
            faceCenters.Add(temp);
        }


        //Remove vertices from calculation that have no interior connections
        int computeVertices = mesh.VertexCount;
        foreach (int vID in mesh.VertexIndices())
        {
            bool hasInteriorEdges = false;
            foreach (int eID in mesh.VtxEdgesItr(vID))
            {
                if (!mesh.IsBoundaryEdge(eID))
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

        
        //StandardMeshWriter.WriteFile(@"C:\Users\Adair\Documents\Codename - GRAINS Multiplayer\Assets\testInput.obj", new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);

        //Find the source and sink nodes
        List<int> sourceVerts = new List<int>();
        List<int> sinkVerts = new List<int>();

        //TODO change to vertex iteration rather than edge iteration
        foreach (int eID in mesh.EdgeIndices())
        {
            if (mesh.IsBoundaryEdge(eID))
            {
                continue;
            }
            Index4i edgeData = mesh.GetEdge(eID);
            Vector3d vertA = mesh.GetVertex(edgeData.a);
            Vector3d vertB = mesh.GetVertex(edgeData.b);


            //Enumerate source/sink nodes
            if(!sourceVerts.Contains(edgeData.a) && !sinkVerts.Contains(edgeData.a))
            {
                if (vertA.x == 0)
                    sourceVerts.Add(edgeData.a);
                if (vertA.x == boundaries.Width)
                    sinkVerts.Add(edgeData.a);
            }
            if(!sourceVerts.Contains(edgeData.b) && !sinkVerts.Contains(edgeData.b))
            {
                if (vertB.x == 0)
                    sourceVerts.Add(edgeData.b);
                if (vertB.x == boundaries.Width)
                    sinkVerts.Add(edgeData.b);
            }

        }

        int totalVertCount = computeVertices - sourceVerts.Count - sinkVerts.Count + 2;


        //Create wieghted Adjacency matrix

        Matrix<double> weightedMatrix = Matrix<double>.Build.Dense(totalVertCount, totalVertCount);
        Dictionary<int, int> vertexMapping = new Dictionary<int, int>(); //Key is DMesh3 vertex index, value is the matrix index

        List<double> edgeDiffusance = new List<double>();
        //List<Index2i> edgeConnectivity = new List<Index2i>();
        double widthBoundary = 5 * Mathf.Pow(10, -10); //All boundaries modeled with a width of 5 angstroms
        int vertIdx = 0;

        //Find the difusance, and replace the edge connectivity with the supernodes, and build the adjacency matrix (step 1)
        foreach (int eID in mesh.EdgeIndices())
        {
            int vertAidx = -1;
            int vertBidx = -1;
            Index4i edgeData = mesh.GetEdge(eID);
            Vector3d vertA = mesh.GetVertex(edgeData.a);
            Vector3d vertB = mesh.GetVertex(edgeData.b);
            Vector3d faceA = mesh.GetTriCentroid(edgeData.c);
            Vector3d faceB;

            if (mesh.IsBoundaryEdge(eID))
            {
                continue;
            }
            else
            {
                faceB = mesh.GetTriCentroid(edgeData.d);
            }
            //Check if current vertex is a source or a sink, if so give it the supernode index
            //If not, give it the mapped matrix index if it exists, or map a brand new vertex
            bool sourceOrSink = false;
            if (sourceVerts.Contains(edgeData.a))
            {
                vertAidx = totalVertCount - 2;
                sourceOrSink = true;
            }
            if (sinkVerts.Contains(edgeData.a))
            {
                vertAidx = totalVertCount-1;
                sourceOrSink = true;
            }
            if (!sourceOrSink)
            {
                if (vertexMapping.ContainsKey(edgeData.a))
                {
                    vertAidx = vertexMapping[edgeData.a];
                }
                else
                {
                    vertAidx = vertIdx;
                    vertexMapping.Add(edgeData.a, vertIdx);
                    vertIdx++;
                }
            }

            sourceOrSink = false;
            if (sourceVerts.Contains(edgeData.b))
            {
                vertBidx = totalVertCount - 2;
                sourceOrSink = true;
            }
            if (sinkVerts.Contains(edgeData.b))
            {
                vertBidx = totalVertCount-1;
                sourceOrSink = true;
            }
            if (!sourceOrSink)
            {
                if (vertexMapping.ContainsKey(edgeData.b))
                {
                    vertBidx = vertexMapping[edgeData.b];
                }
                else
                {
                    vertBidx = vertIdx;
                    vertexMapping.Add(edgeData.b, vertIdx);
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
            //double diffusance = debugDiffusivity * gbArea / length;
            double diffusance = gameController.allFaces[mesh.GetTriangleGroup(mesh.GetEdgeT(eID).a)].GetComponent<GrainMesher>().diffusivity * gbArea / length;
            edgeDiffusance.Add(diffusance);

            //Debugging visualization of SGT vertices
            //Debug.Log("Current Vertex a: " + vertAidx + "\nCurrent Vertex b: " + vertBidx);
            //GameObject vertAGo = Instantiate<GameObject>(debugObject, (Vector3)vertA*1000, Quaternion.identity);
            //vertAGo.name = "Vert: " + vertAidx;
            //GameObject vertBGo = Instantiate<GameObject>(debugObject, (Vector3)vertB*1000, Quaternion.identity);
            //vertBGo.name = "Vert: " + vertBidx;
            weightedMatrix[vertAidx, vertBidx] = diffusance;
            weightedMatrix[vertBidx, vertAidx] = diffusance;
        }

        var colSums = weightedMatrix.ColumnSums();
        Matrix<double> diagColSums = Matrix<double>.Build.DenseOfDiagonalVector(colSums);
        weightedMatrix = diagColSums - weightedMatrix; //Completed adjacency matrix

        //Compute eigendecomposition
        Evd<double> eigen = weightedMatrix.Evd(Symmetricity.Symmetric);

        Vector<double> eigenvalues = eigen.EigenValues.Real();
        var eigenvectors = eigen.EigenVectors;


        //TODO: Compute Effective Diffusivity
        for (int i = 1; i < eigenvalues.Count; i++)
        {
            //Debug.Log("Current Eigenvalue index:" + i);
            Deff += (1 / eigenvalues[i]) * System.Math.Pow((eigenvectors.At(totalVertCount - 2, i) - eigenvectors.At(totalVertCount-1, i)),2);
        }
        Deff = 1 / Deff;

        Deff = Deff * boundaries.Width / (boundaries.Height * boundaries.Depth);

        return Deff;
    }

}
