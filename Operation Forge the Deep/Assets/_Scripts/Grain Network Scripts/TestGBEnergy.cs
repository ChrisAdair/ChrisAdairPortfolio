using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
public class TestGBEnergy : MonoBehaviour {


    public GameObject sphere;
	// Use this for initialization
	void Start () {


        List<Vector3> tiltEng = new List<Vector3>();

        for(float i = 0; i < 90; i+=0.1f)
        {
            //GIVEN
            Quaternion p = Quaternion.identity;
            Quaternion q = Quaternion.AngleAxis(i, new Vector3(0, 1, 0));
           
            Vector<double> N = Vector<double>.Build.Dense(new double[] { 1, 0, 0 });
            //CALCULATIONS
            //Put it into grain frame
            N = N * GBEnergy.Quat2Mat(p).Inverse();

            var disorient = DisorientationFunctions.Disorientation('c', Quaternion.Inverse(p) * q);
            double energy = GBEnergy.GB5DOF(disorient, N, "Ni");

            tiltEng.Add(new Vector3(i * 0.1f, (float)energy*10,0));
        }
        foreach(Vector3 vec in tiltEng)
        {
            Instantiate<GameObject>(sphere, vec, Quaternion.identity);
        }

	}
	

}
