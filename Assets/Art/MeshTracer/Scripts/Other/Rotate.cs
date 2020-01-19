using UnityEngine;
using System.Collections;

namespace MeshTracer
{
	public class Rotate : MonoBehaviour {

		public float speed;
		// Use this for initialization
		void Start () {
		
		}
		
		// Update is called once per frame
		void FixedUpdate () 
		{
			this.transform.Rotate (0, speed * Time.deltaTime, 0);
		}
	}
}
