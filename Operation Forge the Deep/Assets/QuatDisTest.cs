using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuatDisTest : MonoBehaviour
{
    public Quaternion test;
    public Quaternion betterTest;
    public GameObject fromMtex;
    public GameObject fromDisorient;
    public GameObject betterCubic;
    public GameObject referenceCube;

    // Start is called before the first frame update
    void Start()
    {
        referenceCube.transform.rotation *= new Quaternion(0.277633321561201f, 0.366381219287076f, 0.606358401407390f, 0.648855939292602f);
        //Test case from Mtex
        fromMtex.transform.rotation *= new Quaternion(0.305599899925857f, - 0.0656227178055436f, 0.0231251799203319f,  0.949614440774135f);
        //Test case from current Disorient function
        test = DisorientationFunctions.Disorientation('c', new Quaternion(0.277633321561201f, 0.366381219287076f,   0.606358401407390f,   0.648855939292602f));
        fromDisorient.transform.rotation *= test;
        //Test case from new BetterCubic function
        betterTest = DisorientationFunctions.Disorientation('b', new Quaternion(0.277633321561201f, 0.366381219287076f, 0.606358401407390f, 0.648855939292602f));
        betterCubic.transform.rotation *= betterTest;
    }

}
