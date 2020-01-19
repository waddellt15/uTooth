using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dome: MonoBehaviour
{
    //[Tooltip("Background=1000, Geometry=2000, AlphaTest=2450, Transparent=3000, Overlay=4000")]
    //public int queue;
    public float fadeSpeed;
    public bool daylight;
    public float light;
    //public int[] queues;

    //void Start()
    //{
        //caused bug in dome transparency
        //Renderer renderer = GetComponent<Renderer>(); 
        //renderer.sharedMaterial.renderQueue = queue;

        /*
         if (!renderer || !renderer.sharedMaterial || queues == null)
            return;
        renderer.sharedMaterial.renderQueue = queue;
        for (int i = 0; i < queues.Length && i < renderer.sharedMaterials.Length; i++)
            renderer.sharedMaterials[i].renderQueue = queues[i];
            */
    //}

    //simulate daylight and night
    void Update()
    {
            Color color = GameObject.Find("Dome").GetComponent<MeshRenderer>().material.color;
            //color.a -= Time.deltaTime * fadeSpeed;
            color.a = 1 - light/850;
            GameObject.Find("Dome").GetComponent<MeshRenderer>().material.color = color;
            //if (color.a <= 0.0f) {  daylight = false;   }
    }

}