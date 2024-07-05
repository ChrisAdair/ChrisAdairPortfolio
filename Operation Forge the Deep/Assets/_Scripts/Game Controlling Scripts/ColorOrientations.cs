using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
//Attach to player
public class ColorOrientations : NetworkBehaviour {

    public enum Visual
    {
        LabFrame = 0,
        UnitCellFrame = 1,
        BoundaryColor =2
    }

    public Visual type;
    public bool updated = true;
    public Dropdown orientColorMode;
    public Color highlightColor;

    private Button toggleMode;
    [SerializeField]
    private Orientation grainCenter;
    private PlayerBehavior player;

    //Initialize to true to start behavior
    public bool display = true;


    private void Start()
    {
        if (!isLocalPlayer)
            return;
        toggleMode = GameObject.Find("ColorOrient")?.GetComponent<Button>();
        player = GetComponent<PlayerBehavior>();
        grainCenter = player.assignedGrain;
        orientColorMode = GameObject.Find("OrientColorMode")?.GetComponent<Dropdown>();
        orientColorMode?.onValueChanged.AddListener(delegate { ChangeOrientColorMode(orientColorMode); });

        toggleMode?.onClick.AddListener(ToggleOrientColoring);

    }

    public void ToggleOrientColoring()
    {
        grainCenter = player.assignedGrain;
        if(display)
        {
            UpdateColors();
            display = false;
        }
        else
        {
            GameObject[] centers = GameObject.FindGameObjectsWithTag("Orientation");
            if (centers != null)
            {
                foreach (GameObject obj in centers)
                {
                    obj.GetComponent<Renderer>().material.color = Color.white;
                }
            }
            CmdChangeFaceSpecColor(new Color(1,1,1,0.5f));

            grainCenter.GetComponent<Renderer>().material.color = highlightColor;
            display = true;
        }

        
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;
        if (!updated && !display)
        {
            UpdateColors();
            updated = true;
        }
    }

    private void UpdateColors()
    {
        switch (type)
        {
            case Visual.LabFrame:
                GameObject[] centers = GameObject.FindGameObjectsWithTag("Orientation");
                if (centers != null)
                {
                    foreach (GameObject obj in centers)
                    {
                        Quaternion dis = DisorientationFunctions.Disorientation('c', obj.transform.rotation);
                        obj.GetComponent<Renderer>().material.color = DisorientationColoring.DisorientationColor(dis);
                    }
                }

                break;

            case Visual.UnitCellFrame:
                GameObject[] neighbors = GameObject.FindGameObjectsWithTag("Orientation");
                Quaternion origin = player.assignedGrain.transform.rotation;
                foreach (GameObject obj in neighbors)
                {
                    Quaternion dis = DisorientationFunctions.Disorientation('c', Quaternion.Inverse(obj.transform.rotation) * origin);
                    obj.GetComponent<Renderer>().material.color = DisorientationColoring.DisorientationColor(dis);
                }
                player.assignedGrain.gameObject.GetComponent<Renderer>().material.color = Color.white;
                break;

            case Visual.BoundaryColor:

                CmdChangeFaceColor(grainCenter.gameObject);
                break;
        }
    }

    public void ChangeOrientColorMode(Dropdown changed)
    {
        type = (Visual) changed.value;

        //Change back to initial state
        GameObject[] centers = GameObject.FindGameObjectsWithTag("Orientation");
        if (centers != null)
        {
            foreach (GameObject obj in centers)
            {
                obj.GetComponent<Renderer>().material.color = Color.white;
            }
        }
        CmdChangeFaceSpecColor(new Color(1, 1, 1, 0.5f));
        grainCenter.GetComponent<Renderer>().material.color = highlightColor;

        //Re-call the coloring system
        if(!display)
            UpdateColors();
    }

    [Command]
    private void CmdChangeFaceColor(GameObject grainID)
    {

        List<GameObject> faces = grainID.GetComponent<Orientation>().faces;
        List<GameObject> faceGo = new List<GameObject>();
        List<Color> colors = new List<Color>();
        foreach (GameObject go in faces)
        {
            GrainMesher mesh = go.gameObject.GetComponent<GrainMesher>();

            //This needs to reflect the current puzzle
            double outputNormalized = mesh.diffusivity / (System.Math.Abs(System.Math.Pow(10, 7) * (6.28 + Mathf.Sqrt(3)) + 1));
            //double outputNormalized = mesh.diffusivity / 1.4;

            Color c = Color.white;


            if (outputNormalized <= 0.2)
            {
                c.r = 0;
                c.g = 0;
                c.b = 0.6f + 2.0f * (float)outputNormalized;
            }
            else if (outputNormalized <= 0.4 )
            {
                c.r = 0;
                c.g = 5 * ((float)outputNormalized - 0.2f);
            }
            else if (outputNormalized <= 0.6)
            {
                c.r = 5 * ((float)outputNormalized - 0.4f);
                c.b = 5 * (0.6f - (float)outputNormalized);
            }
            else if(outputNormalized <= 0.8)
            {
                c.g = 5 * (0.8f  - (float)outputNormalized);
                c.b = 0;
            }
            else
            {
                c.g = 0;
                c.b = 0;
                c.r = 1 + 2 * (0.8f - (float)outputNormalized);
            }
            c.a = 0.75f;



            colors.Add( new Color(c.r, c.g, c.b, c.a));
            faceGo.Add(go);

        }

        TargetChangeFaceColor(connectionToClient, faceGo.ToArray(), colors.ToArray());
    }
    [Command]
    private void CmdChangeFaceSpecColor(Color color)
    {
        GameObject[] faces = GameObject.FindGameObjectsWithTag("GrainBoundary");
        List<Color> colors = new List<Color>();
        foreach (GameObject go in faces)
        {
            colors.Add(color);
        }

        TargetChangeFaceColor(connectionToClient, faces, colors.ToArray());
    }
    
    

    [TargetRpc]
    private void TargetChangeFaceColor(NetworkConnection conn, GameObject[] faces, Color[] colors)
    {
        for(int i = 0; i < faces.Length; i++)
        {
            faces[i].GetComponent<GrainMesher>().individualMat.color = colors[i];
        }
    }

}
