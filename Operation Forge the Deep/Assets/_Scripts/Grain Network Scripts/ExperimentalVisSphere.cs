using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentalVisSphere : MonoBehaviour {



    public void OnMouseUpAsButton()
    {
        SendMessageUpwards("ConfirmRotation", gameObject);
    }
}
