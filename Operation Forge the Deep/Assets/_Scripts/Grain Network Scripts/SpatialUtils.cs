using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Utilities meant for transforming 3d points into a 2d basis space for use in a 2d triangulation scheme
/// </summary>
public static class SpatialUtils {

    /// <summary>
    /// Transform a list of co-planar 3D points into a 2D basis space
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static Vector2[] WorldtoPlaneSpace(Vector3[] points)
    {
        List<Vector2> temp = new List<Vector2>();
        Vector3 normal = FaceNormal(points[0], points[1], points[2]);
        Vector3 xAxis = FirstBasis(points[0], points[1]);
        Vector3 yAxis = SecondBasis(normal, xAxis);

        foreach(Vector3 vert in points)
        {
            temp.Add(PlanePointTransform(vert, points[0], xAxis, yAxis));
        }
        return temp.ToArray();
    }
    public static Vector2[] WorldtoPlaneSpace(Vector3[] points, out Vector3 firstBasis, out Vector3 secondBasis)
    {
        List<Vector2> temp = new List<Vector2>();
        Vector3 normal = FaceNormal(points[0], points[1], points[2]);
        Vector3 xAxis = FirstBasis(points[0], points[1]);
        Vector3 yAxis = SecondBasis(normal, xAxis);
        firstBasis = xAxis;
        secondBasis = yAxis;

        foreach (Vector3 vert in points)
        {
            temp.Add(PlanePointTransform(vert, points[0], xAxis, yAxis));
        }
        return temp.ToArray();
    }
    public static Vector3 InversePlanePointTransform(Vector2 point, Vector3 origin, Vector3 firstBasis, Vector3 secondBasis)
    {
        Vector3 inverse = Vector3.zero;
        inverse = origin + firstBasis * point.x + secondBasis * point.y;
        return inverse;
    }

    //Private internal methods
    private static Vector3 FaceNormal(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return Vector3.Cross(v2 - v1, v3 - v2).normalized;
    }

    private static Vector3 FirstBasis(Vector3 v1, Vector3 v2)
    {
        return (v2 - v1).normalized;
    }
    private static Vector3 SecondBasis(Vector3 normal, Vector3 firstBasis)
    {
        return Vector3.Cross(normal, firstBasis);
    }
    
    private static Vector2 PlanePointTransform(Vector3 point, Vector3 origin, Vector3 firstBasis, Vector3 secondBasis)
    {
        Vector2 temp = Vector2.zero;
        temp.x = Vector3.Dot(firstBasis, point - origin);
        temp.y = Vector3.Dot(secondBasis, point - origin);

        return temp;
    }


}
