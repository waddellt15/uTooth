using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MeshTracer
{
	public class ThirdPersonCamera : MonoBehaviour
	{
		public GameObject Target;
		public float damping;
		public float tightAngleDamping;
		
		public float minDistance;
		public float maxDistance;
		public float minDistanceSpeed;
		public float maxDistanceSpeed;

		Vector3 offset;
		Vector3 desiredPosition; // the exact location o fwhere we want to be
		Vector3 lerpPosition; // the intermediate position of where we want to be
		float speed; // the speed of our ship
		float distance; // our current distance from ship

		public void Initialize(GameObject camera, GameObject ship)
		{
			offset = (camera.transform.position - ship.transform.position);
			distance = minDistance;
		}

		void Update()
		{
			speed = (Target.GetComponent<Rigidbody>().velocity.magnitude);
			
			//get desired distance from ship
			if (speed > minDistanceSpeed && GetSpeed() < maxDistanceSpeed)
			{
				float percent = speed / maxDistanceSpeed; // percent of maxDistance we want to be
				distance = minDistance + (maxDistance - minDistance) * percent;
			}
			if (GetSpeed() >= maxDistanceSpeed)
			{
				distance = maxDistance;
			}


			this.transform.position = GetLerpPosition(this.gameObject);
			//always look at the ship
			transform.LookAt(Target.transform);
		}

		public Vector3 GetLerpPosition(GameObject camera)
		{
			//get the exact location of the place we want to be
			desiredPosition =  (Target.transform.position + Target.transform.right * offset.x + Target.transform.up * offset.y + Target.transform.forward * offset.z);
			Vector3 toDesired = desiredPosition - Target.transform.position; //ship to desired position
			
			//lerp between where we are, and where we want to be
			Vector3 shipToMe = camera.transform.position - Target.transform.position; // ship to current positoin
			
			//select a damping speed based on the angle. i.e. If we the ship is facing almost a completely different angle than the camera.
			float damp = damping;
			if(Vector3.Dot(camera.transform.forward,Target.transform.forward) < -.25f)
			{
				damp = tightAngleDamping;
	            //Debug.Log("tightAngle");
			}
			
			Vector3 lerpshipToMe = Vector3.Lerp (shipToMe, toDesired, Time.fixedDeltaTime * damp);
			
			Vector3 toMeNormalized = lerpshipToMe.normalized * distance; // 

	        Vector3 position = Target.transform.position + toMeNormalized;

	        Debug.DrawLine(Target.transform.position, desiredPosition, Color.green);
	        Debug.DrawLine(Target.transform.position, position, Color.white);

			//Gizmos.DrawSphere (toMeNormalized, 3);
			//set our position
			
			return position;

			
		}

		public void SetOffset(Vector3 offset)
		{
			this.offset = offset;
		}
		public Vector3 GetOffset()
		{
			return this.offset;
		}
		public void SetDesiredPosition(Vector3 desiredPosition)
		{
			this.desiredPosition = desiredPosition;
		}
		public Vector3 GetDesiredPosition()
		{
			return this.desiredPosition;
		}
		public void SetLerpPosition(Vector3 lerpPosition)
		{
			this.lerpPosition = lerpPosition;
		}
		public Vector3 GetLerpPosition()
		{
			return this.lerpPosition;
		}
		public void SetSpeed(float speed)
		{
			this.speed = speed;
		}
		public float GetSpeed()
		{
			return this.speed;
		}
	}
}


