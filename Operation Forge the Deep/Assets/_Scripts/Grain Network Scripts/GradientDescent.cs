using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;
using UnityEngine.UI;
using System;


public class GradientDescent : MonoBehaviour
{

    public NMSGTMesherandSolver solver;
    public Orientation grainToOptimize;

    public delegate IEnumerator ObjectiveFunction(Action<bool> callback);

    ObjectiveFunction objectiveFunction;
    public float tolerance;
    public float stepSize = 1.0f;

    private Quaternion originalOrientation;
    private Vector3[] rotations;
    private float originalScore;
    private float deltaScore;
    private NTMesh3 savedMesh;
    private GrainNetworkAssigner gameController;

    public void Awake()
    {
        /*rotations = new Vector3[]
        {
            new Vector3(stepSize, 0, 0),
            new Vector3(0, stepSize, 0),
            new Vector3(0, 0, stepSize),
            new Vector3(-stepSize, 0, 0),
            new Vector3(0, -stepSize, 0),
            new Vector3(0, 0, -stepSize),

            new Vector3(stepSize, stepSize, 0),
            new Vector3(stepSize, -stepSize, 0),
            new Vector3(-stepSize, stepSize, 0),
            new Vector3(-stepSize, -stepSize, 0),

            new Vector3(stepSize, 0, stepSize),
            new Vector3(stepSize, 0, -stepSize),
            new Vector3(-stepSize, 0, stepSize),
            new Vector3(-stepSize, 0, -stepSize),

            new Vector3(0, stepSize, stepSize),
            new Vector3(0, stepSize, -stepSize),
            new Vector3(0, -stepSize, stepSize),
            new Vector3(0, -stepSize, -stepSize),

            new Vector3(stepSize, stepSize, stepSize),
            new Vector3(-stepSize, stepSize, stepSize),
            new Vector3(stepSize, -stepSize, stepSize),
            new Vector3(stepSize, stepSize, -stepSize),

            new Vector3(stepSize, -stepSize, -stepSize),
            new Vector3(-stepSize, stepSize, -stepSize),
            new Vector3(-stepSize, -stepSize, stepSize),

            new Vector3(-stepSize, -stepSize, -stepSize)
        };*/

        rotations = new Vector3[]
        {
            new Vector3(stepSize, 0, 0),
            new Vector3(0, stepSize, 0),
            new Vector3(0, 0, stepSize),
            new Vector3(-stepSize, 0, 0),
            new Vector3(0, -stepSize, 0),
            new Vector3(0, 0, -stepSize)
        };
        tolerance = 0.0001f;
        
    }

    public void StartOptimization(Action<bool> callback)
    {
        deltaScore = 0.5f;
        objectiveFunction = OptimizeGrainDiffusivity;
        solver = GameObject.Find("SGTSolver(Clone)").GetComponent<NMSGTMesherandSolver>();
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GrainNetworkAssigner>();
        StartCoroutine(objectiveFunction(callback));

    }
    public IEnumerator OptimizeTotalScore(Action<bool> callback)
    {
        
        originalOrientation = grainToOptimize.transform.rotation;

        savedMesh = GetStaticMesh();
        //Debug.LogError("Got to this point: DScore" + deltaScore + " Tolerance: " + tolerance);
        while (deltaScore > tolerance)
        {
            var timeStart = Time.realtimeSinceStartup;
            Vector3 highestGradient = Vector3.zero;
            float tempDelta = 0;

            //Run gradient based optimization step
            foreach(Vector3 rot in rotations)
            {
                grainToOptimize.transform.Rotate(rot, Space.World);
                grainToOptimize.UpdateDiffusivities();

                double temp = solver.SGTCalculation(savedMesh) - originalScore;
                if (temp > tempDelta)
                {
                    tempDelta = (float)temp;
                    highestGradient = rot;
                }

                grainToOptimize.transform.rotation = originalOrientation;
            }

            //Update optimization step
            grainToOptimize.transform.Rotate(highestGradient, Space.World);
            grainToOptimize.UpdateDiffusivities();
            originalOrientation = grainToOptimize.transform.rotation;
            deltaScore = tempDelta;
            originalScore += deltaScore;
            gameController.SGTOutput = originalScore;
            //Debug.LogError("Finished Iteration. Delta is :" + deltaScore + " \n\r Rotation is : " + highestGradient.ToString());
            Debug.LogError("Single step time: " + (Time.realtimeSinceStartup - timeStart));
            callback.Invoke(false);
            yield return null;
        }

        //Debug.LogError("Finished Iterations");
        //Signal completion to calling function
        callback.Invoke(true);
    }


    public IEnumerator OptimizeGrainDiffusivity(Action<bool> callback)
    {

        originalOrientation = grainToOptimize.transform.rotation;

        savedMesh = GetStaticMesh();
        //Debug.LogError("Got to this point: DScore" + deltaScore + " Tolerance: " + tolerance);
        while (deltaScore > tolerance)
        {
            Vector3 highestGradient = Vector3.zero;
            double firstDiff = grainToOptimize.totDiffusivity;
            double tempDelta = 0;

            //var timeStart = Time.realtimeSinceStartup;
            //Run gradient based optimization step
            foreach (Vector3 rot in rotations)
            {
                grainToOptimize.transform.Rotate(rot, Space.World);
                grainToOptimize.UpdateDiffusivities();

                double temp = grainToOptimize.totDiffusivity;
                if (temp > tempDelta)
                {
                    tempDelta = temp;
                    highestGradient = rot;
                    deltaScore = (float) (temp - firstDiff);
                }

                grainToOptimize.transform.rotation = originalOrientation;
            }

            //Update optimization step
            grainToOptimize.transform.Rotate(highestGradient, Space.World);
            grainToOptimize.UpdateDiffusivities();
            originalOrientation = grainToOptimize.transform.rotation;
            //Debug.LogError("Finished Iteration. Delta is :" + deltaScore + " \n\r Rotation is : " + highestGradient.ToString());

            //Debug.LogError("Single step time: " + (Time.realtimeSinceStartup - timeStart));
            callback.Invoke(false);
            yield return null;
        }

        gameController.sgtUpdated = false;
        //Debug.LogError("Finished Iterations");
        //Signal completion to calling function
        
        callback.Invoke(true);
    }

    //Consider changing this to reflect the internal static mesh of the NMSGT code
    private NTMesh3 GetStaticMesh()
    {
        solver = GameObject.Find("SGTSolver(Clone)").GetComponent<NMSGTMesherandSolver>();

        return solver.sgtMesh;
    }
}
