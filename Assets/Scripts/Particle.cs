    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{
    // Update is called once per frame
    public int temp;
    public Material rend;

    void Update()
    {
        gameObject.GetComponent<ParticleSystem>().Play();
        if (temp == 1)
        {
            Cold();
        } 
        else if (temp == 2)
        {
            Hot();
        }
        else
        {
            Normal();
        }
    }


    private void Cold()
    {
        var main = gameObject.GetComponent<ParticleSystem>().main;
        //rend.color = Color.cyan;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.cyan);
    }

    private void Hot()
    {
        var main = gameObject.GetComponent<ParticleSystem>().main;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.red);
    }

    private void Normal()
    {
        var main = gameObject.GetComponent<ParticleSystem>().main;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white);
    }
}
