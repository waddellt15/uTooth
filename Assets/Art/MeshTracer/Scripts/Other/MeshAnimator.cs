using UnityEngine;
using System.Collections;

namespace MeshTracer
{
	/// <summary>
	/// Turn the mesh renderer on and off
	/// </summary>
	public class MeshAnimator : MonoBehaviour {

		public AnimationCurve fadeCurve;
		public float fadeTime;
		public float onTime;
		public float offTime;

		public bool on = true;
		bool fading = false;
		float stateTimer = 0;
		float fadeTimer = 0;

		MeshRenderer renderer;
		// Use this for initialization
		void Start () 
		{
			renderer = this.GetComponent<MeshRenderer> ();
			//renderer.enabled = on;
		}
		
		// Update is called once per frame
		void Update () 
		{
			renderer.enabled = on;
			if(on)
			{
				if(stateTimer > onTime)
				{
					StopAllCoroutines();
					StartCoroutine(Fade(false)); // fade off
				}
			}
			else
			{
				if(stateTimer > offTime)
				{
					StopAllCoroutines();
					StartCoroutine(Fade(true)); // fade off
				}
			}

			if(!fading)
			{
				stateTimer += Time.deltaTime;
			}
		}

		IEnumerator Fade(bool fadeOn)
		{
			stateTimer = 0;
			fadeTimer = 0;
			fading = true;
			while(fadeTimer < fadeTime)
			{
				float eval = fadeTimer/fadeTime;
				float renderThreshold = fadeCurve.Evaluate(eval);

				if(Random.value < renderThreshold)
				{
					if(fadeOn)
					{
						renderer.enabled = true;
					}
					else{
						renderer.enabled = false;
					}
				}
				else
				{
					if(fadeOn)
					{
						renderer.enabled = false;
					}
					else{
						renderer.enabled = true;
					}
				}
			

				fadeTimer += Time.deltaTime;
				yield return null;
			}

			on = fadeOn;
			renderer.enabled = fadeOn; //make sure we end with the right setting
			fading = false;

		}
	}
}
