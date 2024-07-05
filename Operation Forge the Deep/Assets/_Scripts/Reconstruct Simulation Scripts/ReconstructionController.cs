using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Globalization;
using System.Linq;

public class ReconstructionController : MonoBehaviour {

    public TextAsset VertexList;
    public TextAsset TriList;
    public TextAsset GrainVerts;
    public TextAsset NeighborList;
    public GrainNetworkAssigner assigner;

    public TextAsset reconstructionFile;
    public string structurePath;
    [Header("Data Output")]
    public int computationStepsHuman;
    public int positiveHuman;
    public int negativeHuman;
    public int computationStepsComputer;
    public int positiveOptimization;
    public int negativeOptimization;
    public int positiveUndo;
    public int negativeUndo;
    public float humanCompTime;
    public float compOptTime;
    public float highScore;
    public float highScoreTime;
    //Iterator for reconstruction of time history
    private int reconStep = 0;
    private float prevScore;
    private string[] reconLines;
    private string operationCurrent;
    private float endTime;
    private int numGrains;

	// Use this for initialization
	void Start () {

        /* assigner.VertexList = VertexList;
         assigner.TriList = TriList;
         assigner.GrainVerts = GrainVerts;
         assigner.NeighborList = NeighborList; */
        string fullPath = Application.persistentDataPath + @"/Save Files/" + structurePath + ".SAVE";
        if (structurePath != "")
            assigner.structure = SaveFunctions.LoadFile(Application.persistentDataPath + @"/Save Files/" + structurePath + ".SAVE");

        reconLines = reconstructionFile.text.Split('\n');
        NetworkManager.singleton.StartHost();
        computationStepsHuman = 0;
        computationStepsComputer = 0;
        numGrains = GameObject.FindGameObjectsWithTag("Orientation").Length;
        

	}

    //Old file structure update (Changed Jan 25, 2018)
    //private void Update()
    //{
    //    prevScore = assigner.SGTOutput;
    //    if (reconStep < reconLines.Length-1)
    //    {
    //        string[] reconInst = reconLines[reconStep].Split(' ', '(', ')' ,',','\r');
    //        reconInst = (from data in reconInst where data != "" select data).ToArray();
    //        //string user = reconInst[0];
    //        string operation = operationCurrent = reconInst[1];
    //        switch (operation)
    //        {
    //            case "oriGrain":
    //                int grainNum = int.Parse(reconInst[2]);

    //                float x = float.Parse(reconInst[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                float y = float.Parse(reconInst[4], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                float z = float.Parse(reconInst[5], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                float w = float.Parse(reconInst[6], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);

    //                float beginTime = float.Parse(reconInst[7], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                endTime = float.Parse(reconInst[8], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);

    //                GameObject grainChange = GameObject.Find("Grain " + grainNum);
    //                grainChange.transform.rotation = new Quaternion(x, y, z, w);
    //                assigner.sgtUpdated = false;
    //                grainChange.GetComponent<Orientation>().CmdUpdateDiffusivities();
    //                computationStepsHuman++;
    //                humanCompTime += endTime - beginTime;
    //                break;
    //            case "OptGrain":
    //                grainNum = int.Parse(reconInst[2]);

    //                x = float.Parse(reconInst[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                y = float.Parse(reconInst[4], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                z = float.Parse(reconInst[5], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                w = float.Parse(reconInst[6], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);

    //                int steps = int.Parse(reconInst[7]);
    //                beginTime = float.Parse(reconInst[8], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                endTime = float.Parse(reconInst[9], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);

    //                grainChange = GameObject.Find("Grain " + grainNum);
    //                grainChange.transform.rotation = new Quaternion(x, y, z, w);
    //                assigner.sgtUpdated = false;
    //                grainChange.GetComponent<Orientation>().CmdUpdateDiffusivities();
    //                computationStepsComputer += steps;
    //                computationStepsHuman++;
    //                compOptTime += endTime - beginTime;
    //                break;
    //            case "UndoGrain":
    //                grainNum = int.Parse(reconInst[2]);

    //                x = float.Parse(reconInst[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                y = float.Parse(reconInst[4], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                z = float.Parse(reconInst[5], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
    //                w = float.Parse(reconInst[6], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);

    //                grainChange = GameObject.Find("Grain " + grainNum);
    //                grainChange.transform.rotation = new Quaternion(x, y, z, w);
    //                assigner.sgtUpdated = false;
    //                grainChange.GetComponent<Orientation>().CmdUpdateDiffusivities();
    //                computationStepsHuman++;

    //                break;
    //        }
    //        reconStep++;
    //    }

    //}


    private void Update()
    {
        prevScore = assigner.SGTOutput;
        if(reconStep>1)
            assigner.sgtRemesh = false;
        if (reconStep < reconLines.Length - 1)
        {
            string[] reconInst = reconLines[reconStep].Split(' ', '(', ')', ',', '\r');
            reconInst = (from data in reconInst where data != "" select data).ToArray();
            //string user = reconInst[0];
            string operation = operationCurrent = reconInst[1];
            switch (operation)
            {
                case "ori":
                    //Calculate the computation time
                    float beginTime = float.Parse(reconInst[2], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                    endTime = float.Parse(reconInst[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                    humanCompTime += endTime - beginTime;
                    //Start a loop through the grain orientations
                    int stop = reconStep + numGrains;
                    for (int i = reconStep; i < stop; i++)
                    {
                        reconStep++;
                        reconInst = reconLines[reconStep].Split(' ', '(', ')', ',', '\r');
                        reconInst = (from data in reconInst where data != "" select data).ToArray();
                        int grainNum = int.Parse(reconInst[1]);

                        float w = float.Parse(reconInst[2], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                        float x = float.Parse(reconInst[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                        float y = float.Parse(reconInst[4], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                        float z = float.Parse(reconInst[5], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);

                        GameObject grainChange = GameObject.Find("Grain " + grainNum);
                        grainChange.transform.rotation = new Quaternion(x, y, z, w);
                        grainChange.GetComponent<Orientation>().CmdUpdateDiffusivities();
                    }

                    assigner.sgtUpdated = false;
                    computationStepsHuman++;
                    
                    break;
                case "OptGrain":
                    //Parse the opt line
                    int steps = int.Parse(reconInst[3]);
                    beginTime = float.Parse(reconInst[4], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                    endTime = float.Parse(reconInst[5], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                    computationStepsComputer += steps;
                    computationStepsHuman++;
                    compOptTime += endTime - beginTime;

                    //Parse the multiplayer data
                    stop = reconStep + numGrains;
                    for (int i = reconStep; i < stop; i++)
                    { 
                        reconStep++;
                        reconInst = reconLines[reconStep].Split(' ', '(', ')', ',', '\r');
                        reconInst = (from data in reconInst where data != "" select data).ToArray();
                        int grainNum = int.Parse(reconInst[1]);
                        float w = float.Parse(reconInst[2], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                        float x = float.Parse(reconInst[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                        float y = float.Parse(reconInst[4], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                        float z = float.Parse(reconInst[5], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);

                        GameObject grainChange = GameObject.Find("Grain " + grainNum);
                        grainChange.transform.rotation = new Quaternion(x, y, z, w);
                        grainChange.GetComponent<Orientation>().CmdUpdateDiffusivities();
                    }
                        
                    assigner.sgtUpdated = false;
       
                    break;
                case "Undo":
                    reconStep++;
                    reconInst = reconLines[reconStep].Split(' ', '(', ')', ',', '\r');
                    reconInst = (from data in reconInst where data != "" select data).ToArray();
                    int grainNumU = int.Parse(reconInst[1]);

                    float wu = float.Parse(reconInst[2], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                    float xu = float.Parse(reconInst[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                    float yu = float.Parse(reconInst[4], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);
                    float zu = float.Parse(reconInst[5], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent);

                    GameObject grainChangeU = GameObject.Find("Grain " + grainNumU);
                    grainChangeU.transform.rotation = new Quaternion(xu, yu, zu, wu);
                    assigner.sgtUpdated = false;
                    grainChangeU.GetComponent<Orientation>().CmdUpdateDiffusivities();
                    computationStepsHuman++;

                    break;
            }
            reconStep++;
        }
    }
    private void LateUpdate()
    {
        if (reconStep < reconLines.Length - 1)
        {
            switch (operationCurrent)
            {
                case "ori":
                    if (assigner.SGTOutput > prevScore)
                        positiveHuman++;
                    else
                        negativeHuman++;
                    break;
                case "OptGrain":
                    if (assigner.SGTOutput > prevScore)
                        positiveOptimization++;
                    else
                        negativeOptimization++;
                    break;
                case "Undo":
                    if (assigner.SGTOutput > prevScore)
                        positiveUndo++;
                    else
                        negativeUndo++;
                    break;
            }
            if (assigner.SGTOutput > highScore)
            {
                highScore = assigner.SGTOutput;
                highScoreTime = endTime;
            }
                
        }
            
    }

}
