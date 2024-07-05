using UnityEngine;
using UnityEngine.UI;

public class MenuScript : MonoBehaviour {


    public Canvas menuScreen;
    public PlayerBehavior player;


    public void Start()
    {
        menuScreen.enabled = false;
    }
    public void ToggleMenu()
    {
        menuScreen.enabled = !menuScreen.enabled;
    }
    public void SetCameraSpeed(float cameraSpeed)
    {
        player.cameraSpeed = cameraSpeed;
    }
}
