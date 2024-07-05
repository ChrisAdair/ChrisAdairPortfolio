using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using g3;
using System.Linq;

//Using shape optimization algorithms from Park and Shonts 2012
public class MeshingAlgorithms {


    public DMesh3 MidpointRefinement(DMesh3 mesh)
    {



        return mesh;
    }
    public DMesh3 PatternSearchOptimize(DMesh3 mesh)
    {


        return mesh;
    }

    private Vector3d ShapeExploration(Vector3d currPos, float stepSize)
    {
        Vector3d output = Vector3d.Zero;



        return output;
    }

    /// <summary>
    /// Shape quality determined my inverse mean ratio
    /// </summary>
    /// <param name="vertices">3 vertex positions</param>
    /// <returns></returns>
    public static double ShapeQuality(Vector3[] vertices)
    {
        double quality = 0;
        
        Vector2[] flatShape = SpatialUtils.WorldtoPlaneSpace(vertices);
        double[] input = new double[4];
        input[0] = (flatShape[1].x - flatShape[0].x);
        input[1] = (flatShape[1].y - flatShape[0].y);
        input[2] = (flatShape[2].x - flatShape[0].x);
        input[3] = (flatShape[2].y - flatShape[0].y);
        double[] wInput = { 1, 0, 0.5, Mathf.Sqrt(3) / 2 };
        Matrix<double> shape = Matrix<double>.Build.Dense(2, 2, input);
        Matrix<double> ideal = Matrix<double>.Build.Dense(2, 2, wInput);

        Matrix<double> temp = shape * (ideal.Inverse());
        if (temp.Determinant() == 0)
            return 10;
        quality = temp.FrobeniusNorm() / (2 * temp.Determinant());
        


        return System.Math.Abs(quality);
    }


    /// <summary>
    /// Checks the quality of a list of triangles by the orthogonality of their edges vs. their centroid lines
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="triID"></param>
    /// <returns></returns>
    public static double OrthogonalQuality(ref NTMesh3 mesh, int[] triID)
    {
        double quality = 0;


        //Find a way to get rid of double counting
        for(int i =0; i<triID.Length; i++)
        {
            Vector3d centroid = Vector3d.Zero;
            double dummy;
            Vector3d dummyVec;
            mesh.GetTriInfo(triID[i],out dummyVec, out dummy, out centroid);
            var edges = mesh.GetTriEdges(triID[i]);
            foreach(int j in edges.array)
            {
                //Skip non-manifold edges
                if (mesh.EdgeTrianglesCount(j) > 2)
                    continue;
                var pVerts = mesh.GetEdgeV(j);
                Vector3d pEdge = (mesh.GetVertex(pVerts.b) - mesh.GetVertex(pVerts.a)).Normalized;
                Vector3d vEdge = Vector3d.Zero;
                foreach(int k in mesh.EdgeTrianglesItr(j))
                {
                    if (k == triID[i])
                        continue;
                    Vector3d vCentroid;
                    mesh.GetTriInfo(k, out dummyVec, out dummy, out vCentroid);
                    vEdge = (centroid - vCentroid).Normalized;
                }
                quality += pEdge.Dot(vEdge);

            }
        }

        return quality;
    }
}
