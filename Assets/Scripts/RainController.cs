using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainController : MonoBehaviour
{
    ParticleSystem part;
    AudioSource rainSound;
    AudioEchoFilter echoSound;

    public float fadeTime;
    public bool makeItRain;

    void Start()
    {
        part = gameObject.GetComponent<ParticleSystem>();
        rainSound = gameObject.GetComponent<AudioSource>();
        echoSound = gameObject.GetComponent<AudioEchoFilter>();
       // part.Stop();
        //rainSound.Stop();
        echoSound.enabled = false;
    }

    void Update()
    {

        part = gameObject.GetComponent<ParticleSystem>();
        rainSound = gameObject.GetComponent<AudioSource>();
        echoSound = gameObject.GetComponent<AudioEchoFilter>();

        if (makeItRain)
        {            // || makeItRain
            // show
            //rainSound.Stop();
            echoSound.enabled = true;
            if (!rainSound.isPlaying)
            {
                rainSound.Play();
            }
            //rainSound.Play();
            part.Play();
           // makeItRain = false;
        }
        else {// || !makeItRain)
            // hide
            part.Stop();
            //if (part.isPlaying)
            //    return;

            //StartCoroutine(FadeOut(rainSound, fadeTime));
            rainSound.Stop();
            echoSound.enabled = false;                
        }
    }

    public static IEnumerator FadeOut(AudioSource audioSource, float FadeTime)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / FadeTime;

            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }


}
