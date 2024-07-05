using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowGrainBoundaries : MonoBehaviour {


    private bool showBoundaries = true;
    // Use this for initialization
    public void OnShowBoundaries()
    {
        GameObject[] boundaries =  GameObject.FindGameObjectsWithTag("GrainBoundary");
        GameObject[] connectors = GameObject.FindGameObjectsWithTag("GrainConnection");
        GameObject[] orientations = GameObject.FindGameObjectsWithTag("Orientation");
        if (showBoundaries)
        {
            foreach (GameObject bound in boundaries)
            {
                bound.GetComponent<MeshRenderer>().enabled = true;
            }
            foreach(GameObject conn in connectors)
            {
                conn.GetComponent<MeshRenderer>().enabled = false;
            }
            foreach (GameObject ori in orientations)
            {
                ori.GetComponent<MeshRenderer>().enabled = false;
            }
            showBoundaries = false;
        }
        else
        {
            foreach (GameObject bound in boundaries)
            {
                bound.GetComponent<MeshRenderer>().enabled = false;
            }
            foreach (GameObject conn in connectors)
            {
                conn.GetComponent<MeshRenderer>().enabled = true;
            }
            foreach (GameObject ori in orientations)
            {
                ori.GetComponent<MeshRenderer>().enabled = true;
            }
            showBoundaries = true;
        }
            

    }
}
