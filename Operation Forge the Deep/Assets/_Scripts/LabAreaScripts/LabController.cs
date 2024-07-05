using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LabController : MonoBehaviour
{
    
    public Button ChalToRaid;
    public Button RaidToChal;
    public Button ChalToComp;
    public Button CompToChal;
    public Button ToMainMenu;
    public GameObject CanvasToAnimate;

    private Animator animator;

    void Start()
    {
        //Set up the animator for the scene control
        animator = CanvasToAnimate.GetComponent<Animator>();

        ChalToRaid.onClick.AddListener(delegate { animator.SetTrigger("ChalToRaid"); });
        RaidToChal.onClick.AddListener(delegate { animator.SetTrigger("RaidToChal"); });
        ChalToComp.onClick.AddListener(delegate { animator.SetTrigger("ChalToComp"); });
        CompToChal.onClick.AddListener(delegate { animator.SetTrigger("CompToChal"); });
        ToMainMenu.onClick.AddListener(delegate 
        {   animator.SetTrigger("ChalToOcean");
            StartCoroutine(WaitToMainMenu()); 
        });
    }

    public IEnumerator WaitToMainMenu()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("Client");
    }
}
