using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentalVisualization : MonoBehaviour {

    public Vector3 UICenter;
    public GameObject rotationTest;
    public Slider angleSlider;
    public Dictionary<GameObject, Vector4> hyperSphere;
    public Orientation first;
    public Orientation second;
    public float scaling;

    public List<Quaternion> debugMisorient;
    public List<Quaternion> debugSpaceRot;

    private bool _sphereComplete;
    private bool _energySpaceEnabled;
    private GrainMesher currentFace;

	// Use this for initialization
	void Start () {
        //Create Hypersphere
        _sphereComplete = false;
        hyperSphere = new Dictionary<GameObject, Vector4>();
        StartCoroutine("CreateHypersphere");
        scaling = 5;
        debugMisorient = new List<Quaternion>();
        debugSpaceRot = new List<Quaternion>();
	}



    public IEnumerator CreateHypersphere()
    {
        for (float r = 1f; r < 10f; r++)
        {
            for (float theta = -Mathf.PI; theta < Mathf.PI; theta += Mathf.PI / 4)
            {
                for (float phi = -Mathf.PI / 2; phi < Mathf.PI / 2; phi += Mathf.PI / 4)
                {
                    float x = r * Mathf.Sin(theta) * Mathf.Cos(phi) + UICenter.x;
                    float y = r * Mathf.Sin(theta) * Mathf.Sin(phi) + UICenter.y;
                    float z = r * Mathf.Cos(theta) + UICenter.z;

                    GameObject point = Instantiate<GameObject>(rotationTest, new Vector3(x, y, z), Quaternion.identity,transform);
                    //Store in axis-angle format
                    hyperSphere.Add(point, new Vector4(x,y,z,r));
                    
                    yield return null;
                }
            }
        }
        _sphereComplete = true;

    }

    public void FindRotationSpace()
    {
        if(_sphereComplete && first !=null && second !=null)
        {
            double output = 0;
            foreach (GameObject go in hyperSphere.Keys)
            {
                Quaternion spaceRotation = Quaternion.AngleAxis(hyperSphere[go].w*scaling, new Vector3(hyperSphere[go].x, hyperSphere[go].y, hyperSphere[go].z).normalized);
                Quaternion misorientation = Quaternion.Inverse(first.orientation * spaceRotation) * second.orientation;
                debugMisorient.Add(misorientation);
                debugSpaceRot.Add(spaceRotation);
                Vector3 normal = currentFace.normal;
                output = (misorientation.x + misorientation.y + misorientation.z + misorientation.w + normal.x + normal.y + normal.z) + 1;
                //output = System.Math.Pow(10, 7);
                //Normalized by the maximum value
                double outputNormalized = output / ((4 + Mathf.Sqrt(3)) + 1);

                Color lerpColor = Color.white;
                if (outputNormalized <= 0.5)
                {
                    lerpColor = Color.Lerp(new Color(0, 0, 1, 0.5f), new Color(1, 0.5f, 0.016f, 0.5f), (float)outputNormalized * 2);

                }
                else
                {
                    lerpColor = Color.Lerp(new Color(1, 0.5f, 0.016f, 0.5f), new Color(1, 0, 0, 0.5f), (float)(outputNormalized - 0.5) * 2);
                }
                go.GetComponent<Renderer>().material.color = lerpColor;
            }
        }
    }

    public void ConfirmRotation(GameObject go)
    {
        first.transform.rotation = first.transform.rotation * Quaternion.AngleAxis(hyperSphere[go].w * scaling, new Vector3(hyperSphere[go].x, hyperSphere[go].y, hyperSphere[go].z).normalized);
    }

    public void EnergySpaceEnabled(bool enabled)
    {
        _energySpaceEnabled = enabled;
    }
    public bool SpaceActive()
    {
        return _energySpaceEnabled;
    }

    public void SetCurrentFace(GrainMesher mesher)
    {
        currentFace = mesher;
        Orientation temp;
        first = mesher.neighbors[0].objectBase.GetComponent<Orientation>();
        second = mesher.neighbors[1].objectBase.GetComponent<Orientation>();
        if(!first.hasAuthority)
        {
            if (!second.hasAuthority)
            {
                first = second = null;
            }
            else
            {
                temp = first;
                first = second;
                second = temp;
            }
        }   

        FindRotationSpace();
    }
}
