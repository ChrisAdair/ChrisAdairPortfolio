using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoneTagBehavior : MonoBehaviour
{
    public float euphoticEnd;
    public float dysphoticEnd;
    public float aphoticEnd;
    public Image activeImage;
    public Sprite euphotic;
    public Sprite dysphotic;
    public Sprite aphotic;
    public Transform camT;

    void Start()
    {
        activeImage = GetComponent<Image>();
        camT = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (camT.position.y > euphoticEnd)
            activeImage.sprite = euphotic;
        else if (camT.position.y > dysphoticEnd)
            activeImage.sprite = dysphotic;
        else if (camT.position.y > aphoticEnd)
            activeImage.sprite = aphotic;
    }
}
