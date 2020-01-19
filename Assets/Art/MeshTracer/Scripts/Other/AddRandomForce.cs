using UnityEngine;
using System.Collections;

namespace MeshTracer
{
	public class AddRandomForce : MonoBehaviour {

		public float force;

		// Use this for initialization
		void Start () {
			this.GetComponent<Rigidbody> ().AddForce (Random.insideUnitSphere * force, ForceMode.Impulse);
		}

	}
}
