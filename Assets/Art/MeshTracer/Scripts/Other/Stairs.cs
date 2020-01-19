using UnityEngine;
using System.Collections;

namespace MeshTracer
{
	public class Stairs : MonoBehaviour {

		//distance player is moved up onColision
		public Player player;
		public Transform movePoint;
		public float lerpSpeed;

		void Start ()
		{

		}

		void OnTriggerEnter(Collider col)
		{
			if(!player.usingStairs)
			{
				StopAllCoroutines ();
				StartCoroutine(MovePlayer(col.gameObject));
			}
		}

		IEnumerator MovePlayer(GameObject go)
		{
			Vector3 toPoint = movePoint.position - go.transform.position;
			float distance = toPoint.magnitude;

			player.usingStairs = true;
			Debug.Log ("moving");
			while(distance > 55f)
			{
				distance = Vector3.Distance(player.transform.position,movePoint.position);
				Debug.Log("dist: " + distance);
				go.transform.position = Vector3.Lerp(go.transform.position,movePoint.position,lerpSpeed*Time.smoothDeltaTime);

				yield return null;
			}
			Debug.Log ("done");
			player.usingStairs = false;
		}
	}
}
