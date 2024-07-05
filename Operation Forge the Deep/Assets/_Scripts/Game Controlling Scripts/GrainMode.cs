using UnityEngine;
using UnityEngine.UI;

public class GrainMode : MonoBehaviour {

    public PlayerBehavior player;

    private bool grainMode = false;

    public void ToggleGrainMode()
    {
        if (!grainMode)
        {
            //Show the entire microstructure so the player can select any grain
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("GrainBoundary"))
            {
                //go.GetComponent<Renderer>().enabled = true;
            }
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Orientation"))
            {
                go.GetComponent<Renderer>().enabled = true;
            }
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Vertex"))
            {
                //go.GetComponent<Renderer>().enabled = true;
            }
            foreach (GameObject orient in GameObject.FindGameObjectsWithTag("Orientation"))
            {
                orient.GetComponent<Orientation>().highlightFaces = true;
            }
            player.DisplayFreeGrains();
            grainMode = true;
            player.selection = eSelected.grain;
        }
        else
        {
            foreach (GameObject orient in GameObject.FindGameObjectsWithTag("Orientation"))
            {
                orient.GetComponent<Orientation>().highlightFaces = false;
            }
            player.HideFreeGrains();
            grainMode = false;
            player.selection = eSelected.orientation;
        }
    }

}
