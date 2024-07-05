using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using UnityEngine;

public class ElasticModulusModel : NetworkBehaviour
{
    public GrainNetworkAssigner assigner;
    public List<Orientation> allGrains;
    public double c11, c12, c44;
    [SyncVar]
    public double elasticModulus;

    private Matrix<double> _C;
    private Matrix<double> stiffnessTensor;

    public void Start()
    {
        stiffnessTensor = Matrix<double>.Build.Dense(6, 6);
        allGrains = assigner.allGrains;
        //Build single crystal stiffness matrix and populate it
        _C = Matrix<double>.Build.Dense(6, 6);
        _C[0, 0] = _C[1, 1] = _C[2, 2] = c11;
        _C[0, 1] = _C[0, 2] = _C[1, 0] = _C[2, 0] = _C[2, 1] = _C[1, 2] = c12;
        _C[3, 3] = _C[4, 4] = _C[5, 5] = c44;
        //Calculate initial elasticity modulus
        UpdateElasticity();
    }

    public void RebuildStiffnessMatrix()
    {
        _C[0, 0] = _C[1, 1] = _C[2, 2] = c11;
        _C[0, 1] = _C[0, 2] = _C[1, 0] = _C[2, 0] = _C[2, 1] = _C[1, 2] = c12;
        _C[3, 3] = _C[4, 4] = _C[5, 5] = c44;
    }
    //Using model from [1] M. Kamaya, Int. J. Solids Struct. 46 (2009) 2642–2649.
    public void UpdateElasticity()
    {
        stiffnessTensor.Clear();
        //Equation is Cv = 1/TotVol * sum(GrainVolume * LocalRotationMatrix^-1 * GlobalStiffnessMatrix * LocalRotationMatrix)
        foreach(Orientation grain in allGrains)
        {
            stiffnessTensor += grain.GrainVolume * QuatToInverse4thOrderTensor(grain.orientation) * _C * QuatTo4thOrderTensor(grain.orientation);
        }
        stiffnessTensor /= 0.000001;
        //The equations following depend on the Voigt Average of the stiffness tensor
        //which has a completely different equation for the elastic modulus
        elasticModulus = (stiffnessTensor[0, 0] - stiffnessTensor[0, 1] + 3 * stiffnessTensor[3, 3]) * (stiffnessTensor[0, 0] + 2 * stiffnessTensor[0, 1]) / (2 * stiffnessTensor[0, 0] + 3 * stiffnessTensor[0, 1] + stiffnessTensor[3, 3]);
        //Correcting for anisotropy in different materials
        double Evo = (c11 - c12 + 3 * c44) * (c11 + 2 * c12) / (2 * c11 + 3 * c12 + c44);
        double A = 2 * c44 / (c11 - c12);
        elasticModulus = elasticModulus - (-0.0048 * Math.Pow(A, 2) + 0.068 * A - 0.075) / Evo;
        assigner.ElasticOutput = (float)elasticModulus;
    }

    private Matrix<double> QuatTo4thOrderTensor(Quaternion qinput)
    {
        Quaternion q = DisorientationFunctions.Disorientation('c', qinput);
        double l1 = Math.Pow(q.w, 2) + Math.Pow(q.x, 2) - Math.Pow(q.y, 2) - Math.Pow(q.z, 2);
        double m1 = 2 * (q.x * q.y - q.z * q.w);
        double n1 = 2 * (q.x * q.z + q.y * q.w);
        double l2 = 2 * (q.x * q.y + q.z * q.w);
        double m2 = Math.Pow(q.w, 2) - Math.Pow(q.x, 2) + Math.Pow(q.y, 2) - Math.Pow(q.z, 2);
        double n2 = 2 * (q.y * q.z - q.x * q.w);
        double l3 = 2 * (q.x * q.z - q.y * q.w);
        double m3 = 2 * (q.y * q.z + q.x * q.w);
        double n3 = Math.Pow(q.w, 2) - Math.Pow(q.x, 2) - Math.Pow(q.y, 2) + Math.Pow(q.z, 2);

        /*Transform matrix from 
        Minhang Bao,
        Chapter 6 - Piezoresistive sensing,
        Editor(s): Minhang Bao,
        Analysis and Design Principles of MEMS Devices,
        Elsevier Science, 2005, Pages 247 - 304,*/

        Matrix<double> alpha = Matrix<double>.Build.Dense(6, 6);
        alpha[0, 0] = Math.Pow(l1, 2);
        alpha[0, 1] = Math.Pow(m1, 2);
        alpha[0, 2] = Math.Pow(n1, 2);
        alpha[1, 0] = Math.Pow(l2, 2);
        alpha[1, 1] = Math.Pow(m2, 2);
        alpha[1, 2] = Math.Pow(n2, 2);
        alpha[2, 0] = Math.Pow(l3, 2);
        alpha[2, 1] = Math.Pow(m3, 2);
        alpha[2, 2] = Math.Pow(n3, 2);

        alpha[0, 3] = 2 * m1 * n1;
        alpha[0, 4] = 2 * n1 * l1;
        alpha[0, 5] = 2 * l1 * m1;
        alpha[1, 3] = 2 * m2 * n2;
        alpha[1, 4] = 2 * n2 * l2;
        alpha[1, 5] = 2 * l2 * m2;
        alpha[2, 3] = 2 * m3 * n3;
        alpha[2, 4] = 2 * n3 * l3;
        alpha[2, 5] = 2 * l3 * m3;

        alpha[3, 0] = l2 * l3;
        alpha[3, 1] = m2 * m3;
        alpha[3, 2] = n3 * n3;
        alpha[4, 0] = l3 * l1;
        alpha[4, 1] = m3 * m1;
        alpha[4, 2] = n3 * n1;
        alpha[5, 0] = l1 * l2;
        alpha[5, 1] = m1 * m2;
        alpha[5, 2] = n1 * n2;

        alpha[3, 3] = m2 * n3 + m3 * n2;
        alpha[3, 4] = n2 * l3 + n3 * l2;
        alpha[3, 5] = m2 * l3 + m3 * l2;
        alpha[4, 3] = m3 * n1 + m1 * n3;
        alpha[4, 4] = n3 * l1 + n1 * l3;
        alpha[4, 5] = m3 * l1 + m1 * l3;
        alpha[5, 3] = m1 * n2 + m2 * n1;
        alpha[5, 4] = n1 * l2 + n2 * l1;
        alpha[5, 5] = m1 * l2 + m2 * l1;


        return alpha;
    }

    private Matrix<double> QuatToInverse4thOrderTensor(Quaternion qinput)
    {
        Quaternion q = DisorientationFunctions.Disorientation('c', qinput);
        double l1 = Math.Pow(q.w, 2) + Math.Pow(q.x, 2) - Math.Pow(q.y, 2) - Math.Pow(q.z, 2);
        double m1 = 2 * (q.x * q.y - q.z * q.w);
        double n1 = 2 * (q.x * q.z + q.y * q.w);
        double l2 = 2 * (q.x * q.y + q.z * q.w);
        double m2 = Math.Pow(q.w, 2) - Math.Pow(q.x, 2) + Math.Pow(q.y, 2) - Math.Pow(q.z, 2);
        double n2 = 2 * (q.y * q.z - q.x * q.w);
        double l3 = 2 * (q.x * q.z - q.y * q.w);
        double m3 = 2 * (q.y * q.z + q.x * q.w);
        double n3 = Math.Pow(q.w, 2) - Math.Pow(q.x, 2) - Math.Pow(q.y, 2) + Math.Pow(q.z, 2);

        /*Transform matrix from 
        Minhang Bao,
        Chapter 6 - Piezoresistive sensing,
        Editor(s): Minhang Bao,
        Analysis and Design Principles of MEMS Devices,
        Elsevier Science, 2005, Pages 247 - 304,*/

        Matrix<double> alpha = Matrix<double>.Build.Dense(6, 6);
        alpha[0, 0] = Math.Pow(l1, 2);
        alpha[1, 0] = Math.Pow(m1, 2);
        alpha[2, 0] = Math.Pow(n1, 2);
        alpha[0, 1] = Math.Pow(l2, 2);
        alpha[1, 1] = Math.Pow(m2, 2);
        alpha[2, 1] = Math.Pow(n2, 2);
        alpha[0, 2] = Math.Pow(l3, 2);
        alpha[1, 2] = Math.Pow(m3, 2);
        alpha[2, 2] = Math.Pow(n3, 2);

        alpha[0, 3] = 2 * l2 * l3;
        alpha[0, 4] = 2 * l3 * l1;
        alpha[0, 5] = 2 * l1 * l2;
        alpha[1, 3] = 2 * m2 * m3;
        alpha[1, 4] = 2 * m3 * m1;
        alpha[1, 5] = 2 * m1 * m2;
        alpha[2, 3] = 2 * n2 * n3;
        alpha[2, 4] = 2 * n3 * n1;
        alpha[2, 5] = 2 * n1 * n2;

        alpha[3, 0] = m1 * n1;
        alpha[3, 1] = m2 * n2;
        alpha[3, 2] = m3 * n3;
        alpha[4, 0] = n1 * l1;
        alpha[4, 1] = n2 * l2;
        alpha[4, 2] = n3 * l3;
        alpha[5, 0] = l1 * m1;
        alpha[5, 1] = l2 * m2;
        alpha[5, 2] = l3 * m3;

        alpha[3, 3] = m2 * n3 + m3 * n2;
        alpha[3, 4] = m3 * n1 + m1 * n3;
        alpha[3, 5] = m1 * n2 + m2 * n1;
        alpha[4, 3] = n2 * l3 + n3 * l2;
        alpha[4, 4] = n3 * l1 + n1 * l3;
        alpha[4, 5] = n1 * l2 + n2 * l1;
        alpha[5, 3] = m2 * l3 + m3 * l2;
        alpha[5, 4] = m3 * l1 + m1 * l3;
        alpha[5, 5] = m1 * l2 + m2 * l1;


        return alpha;
    }
}
