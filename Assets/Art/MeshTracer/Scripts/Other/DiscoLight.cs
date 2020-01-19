using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MeshTracer
{
	public class DiscoLight : MonoBehaviour {

		public List<Color> colors;
		public float minChangeTime;
		public float maxChangeTime;
		float changeTime;
		public float changeTimer = 0;

		Light lightSource;

		// Use this for initialization
		void Start () {
			lightSource = this.GetComponent<Light> ();
			changeTime = Random.Range (minChangeTime, maxChangeTime);
		}
		
		// Update is called once per frame
		void Update () 
		{
			changeTimer += Time.fixedDeltaTime;
			if(changeTimer > changeTime)
			{
				lightSource.color = colors[Random.Range(0,colors.Count-1)];
				changeTime = Random.Range (minChangeTime, maxChangeTime);
				changeTimer = 0;
			}

		}
	}
}
