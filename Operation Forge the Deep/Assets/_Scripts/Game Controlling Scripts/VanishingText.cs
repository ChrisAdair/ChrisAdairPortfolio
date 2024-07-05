using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VanishingText : MonoBehaviour {

    public float lifeTime = 5.0f;
    public Color original;
    public string displayText;
    public Text text;


    private float startTime;
    
	// Use this for initialization
	void Start () {

        text = GetComponent<Text>();
        startTime = Time.time;
        text.text = displayText;
        text.rectTransform.position = Input.mousePosition;
        transform.SetParent(GameObject.FindGameObjectWithTag("UserInterface").transform);
	}
	
	// Update is called once per frame
	void Update () {

        if (Time.time - startTime >= lifeTime)
            Destroy(gameObject);
        else
        {
            float t = Time.time - startTime;
            text.color = Color.Lerp(original, Color.clear, t / lifeTime);
        }
	}
}
