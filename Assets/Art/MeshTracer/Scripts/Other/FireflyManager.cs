using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MeshTracer
{
	public class FireflyManager : MonoBehaviour
	{

		public List<GameObject> fireflies;
		public float G = 3.0f;//6.67f * Mathf.Pow (10, -11);
		private float mass;

		// Use this for initialization
		void Start () 
		{
			if(fireflies.Count > 0)
			{
				mass = fireflies [0].GetComponent<Rigidbody> ().mass;
			}
			else{
				mass = 1;
			}
		}
		
		// Update is called once per frame
		void Update () 
		{
			foreach(GameObject firefly in fireflies)
			{
				ApplyForce(firefly);
			}
		}

		void ApplyForce(GameObject fly)
		{
			Vector3 force = Vector3.zero;

			foreach(GameObject firefly in fireflies)
			{
				if(fly != firefly)
				{
					Vector3 direction = firefly.transform.position - fly.transform.position;
					float distance = direction.magnitude;
					float f = (G*(mass*mass))/(distance);
					force += f*direction.normalized;

					//Debug.Log ("adding force: " + force + " f: " + f + " m: " + mass + " dist: " + distance + " direction: " + direction.normalized);
				}
			}


			fly.GetComponent<Rigidbody> ().AddForce (force);
		}


		//Fg = (Gm1*m2)/r2 // G = 6.67*10^-11
	}
}
