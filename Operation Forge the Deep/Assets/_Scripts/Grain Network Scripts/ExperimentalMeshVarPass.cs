using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentalMeshVarPass : MonoBehaviour {

    private ExperimentalVisualization main;
    // Use this for initialization
    private void Start()
    {
        main = GameObject.Find("Hypersphere").GetComponent<ExperimentalVisualization>();
        //gameObject.GetComponent<MeshCollider>().sharedMesh = gameObject.GetComponent<MeshFilter>().mesh;
    }

    //public void Update()
    //{
    //    updated = GetComponent<GrainMesher>().updated;
    //    if (!updated)
    //    {
    //        gameObject.GetComponent<MeshCollider>().sharedMesh = gameObject.GetComponent<MeshFilter>().mesh;
    //    }
    //}
    private void OnMouseUpAsButton()
    {
        if (main.SpaceActive())
        {
            main.SetCurrentFace(GetComponent<GrainMesher>());
        }
    }
}
