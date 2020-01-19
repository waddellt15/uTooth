using UnityEngine;
using System.Collections;

namespace MeshTracer
{
	public class Orbit : MonoBehaviour {


		public float rotationSpeed;
		public Transform origin;
		public bool randomVector = true;

		Vector3 axis;

		// Use this for initialization
		void Start () 
		{
			if(randomVector)
			{
				Vector3 toOrigin = origin.position - this.transform.position;
				Vector3 randomVec = Random.insideUnitSphere;

				axis = Vector3.Cross (toOrigin, randomVec);
			}
			else{
				axis = Vector3.up;
			}
		}
		
		// Update is called once per frame
		void Update () 
		{
			this.transform.RotateAround (origin.position, axis, rotationSpeed * Time.smoothDeltaTime);
		}
	}
}
