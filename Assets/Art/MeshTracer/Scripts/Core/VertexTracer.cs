using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MeshTracer
{
	public class VertexTracer : MonoBehaviour {

		public TraceAnimation traceAnimation; // controls how the tracer moves across the mesh
		public GameObject particleSystemPrefab; // the particle system to use. Use something with a TrailRenderer attached 
		public bool onStart = true; // play the effect in the OnStart() method
		public bool hideMesh = true; // hide the mesh
		public bool animatedMesh = false; // is this mesh animated? if so it will re-assign vertex locations during the animations
		public bool cameraCulling = true; // if true, only play effects if this object is within view of the main camera.
		/// <summary>
		/// The type of animation, i.e. the path the tracer takes.
		/// The script works by moving the 'particleSystemPrefab' along each vertex, after they've been orderer
		/// Trace_Natural - don't order the verticies
		/// Trace_Random - randomly order the verticies
		/// Trace_Direction - order the verticies by x,y, or z.
		/// </summary>
		public Type type = Type.TRACE_NATURAL;
		public enum Type{TRACE_NATURAL,TRACE_RANDOM,TRACE_RIGHT,TRACE_LEFT,TRACE_UP,TRACE_DOWN,TRACE_FORWARD,TRACE_BACK};
		public int numTracers = 1; // number of vertex tracers
		public bool randStartPos = false; // start each tracer at a random position


		private Type lastType;
		private bool playEffect = false; // play the effect
		private Mesh mesh; // the mesh
		private Vector3[] verticies; //mesh.verticies
		private List<GameObject> pooledParticles; //list of pooled 'particleSystemPrefab's
		private float animationTimer = 0;
		private Camera camera; //camera used for effect culling
		private Plane[] planes; //camera frustrum planes
		private Bounds bounds; // this object's bounds. Set to Vector3.one for simplicity
		private bool objectVisible = true; //is this object currently visible?
		private Collider collider; // this object's collider component
		private SkinnedMeshRenderer skin; // mesh renderer for animated objects
		private Mesh bakedMesh; // baked mesh vertex values, for animated meshes

		//monobehaviors______________________________________________________
		void Start () 
		{
			camera = Camera.main;
			collider = this.GetComponent<Collider> ();

			skin = this.GetComponent<SkinnedMeshRenderer>();
			if(skin != null)
			{
				mesh = this.GetComponent<SkinnedMeshRenderer>().sharedMesh;
				bakedMesh = new Mesh();
				skin.BakeMesh(bakedMesh);
			}
			else
			{
				mesh = this.GetComponent<MeshFilter> ().mesh;
			}
			verticies = mesh.vertices;

			if(particleSystemPrefab.GetComponent<TrailRenderer>() == null)
			{
				Debug.LogWarning("Warning, the particle tracer on : " + this.gameObject.name + " does not have a TrailRenderer attached");
			}

			lastType = type;

			PoolParticles(); //pool the particles/tracers to be used

			if(onStart)
			{
				PlayEffect();
			}

		}


		void Update()
		{
			if(cameraCulling)
			{
				//make sure we have a camera
				if (camera == null)
				{
					Debug.LogError ("No Main Camera! Tag a camera as 'Main Camera' to use for camera culling");
					return;
				}
				//update the camera's frustrum planes
				planes = GeometryUtility.CalculateFrustumPlanes(camera);
				//update the bounds of this object
				if(collider == null) // if we don't have a collider
				{
					//just use this position, and a size of (1,1,1)
					bounds = new Bounds(this.transform.position,Vector3.one);
				}
				else // if we have a collider, use the collider bounds
				{ 
					bounds = collider.bounds;
				}

				objectVisible = GeometryUtility.TestPlanesAABB(planes, bounds);
			}
			else{
				objectVisible = true;
			}

			//if we're animating the mesh,update the vertex locaitons
			if(animatedMesh)
			{
				bakedMesh = new Mesh();
				skin.BakeMesh(bakedMesh);
				verticies = bakedMesh.vertices;
				OrderVerticies();
			}


			if(lastType != type)
			{
				OrderVerticies();
			}

			lastType = type;
		}

		/// <summary>
		/// plays the appropriate efffect, based on Type
		/// Trace_Direction cases order the verticies before starting the Trace() routine
		/// </summary>
		public void PlayEffect()
		{
			if(hideMesh)
			{
				if(this.GetComponent<MeshRenderer>())
				{
					this.GetComponent<MeshRenderer>().enabled = false;
				}
				if(this.GetComponent<SkinnedMeshRenderer>())
				{
					this.GetComponent<SkinnedMeshRenderer>().enabled = false;
				}
			}
			StopAllCoroutines ();
			OrderVerticies ();
			StartCoroutine (Trace ());
		}


		void OrderVerticies()
		{
			switch(type)
			{
			case Type.TRACE_NATURAL:
				break;
			case Type.TRACE_RANDOM:
				//compile a list of random verticies
				List<Vector3> randomVerts = new List<Vector3>();
				List<Vector3> remainingVerts = new List<Vector3>();
				remainingVerts.AddRange(verticies.ToList<Vector3>());
				
				for(int i = 0; i < verticies.Length; i++)
				{
					int index = Random.Range(0,remainingVerts.Count-1);
					randomVerts.Add(remainingVerts[index]);
					remainingVerts.RemoveAt(index);
				}
				
				//randomVerts
				verticies = randomVerts.ToArray();
				break;
			case Type.TRACE_RIGHT:
				//compile a list of ordererd verticies, by x
				verticies = verticies.OrderBy(vert => vert.x).ToArray();
				
				break;
			case Type.TRACE_LEFT:
				//compile a list of ordererd verticies, by -x
				verticies = verticies.OrderBy(vert => -vert.x).ToArray();
				
				break;
			case Type.TRACE_UP:
				//compile a list of ordererd verticies, by y
				verticies = verticies.OrderBy(vert => vert.y).ToArray();
				
				break;
			case Type.TRACE_DOWN:
				//compile a list of ordererd verticies, by -y
				verticies = verticies.OrderBy(vert => -vert.y).ToArray();
				
				break;
			case Type.TRACE_FORWARD:
				//compile a list of ordererd verticies, by z
				verticies = verticies.OrderBy(vert => vert.z).ToArray();
				
				break;
			case Type.TRACE_BACK:
				//compile a list of ordererd verticies, by -z
				verticies = verticies.OrderBy(vert => -vert.z).ToArray();
				
				break;
			default:
				break;
			}
		}

		//iEnumerators______________________________________________________
		/// <summary>
		/// Move the tracers along each vertex, using the order in which they were pased in.
		/// </summary>
		/// <param name="points">Points.</param>
		IEnumerator Trace()
		{

			playEffect = true;
			//EnableParticles (true);
			animationTimer = 0;
			//initialize our tracers
			List<GameObject> tracers = new List<GameObject> ();
			List<int> indicies = new List<int> ();
			for(int i = 0; i < numTracers; i++)
			{
				GameObject tracer = pooledParticles [i];
				tracer.SetActive (true);
				tracer.hideFlags = HideFlags.HideInHierarchy;
				tracers.Add(tracer);
				int index = 0;
				if(randStartPos)
				{
					index = Random.Range(0,verticies.Length);
				}
				indicies.Add(index); // start at a random place inside our array
			}

			
			while(playEffect)
			{
				//play the animation effect
				if(traceAnimation.play && objectVisible)
				{
					//loop the animation
					if(animationTimer > traceAnimation.animTime)
					{
						animationTimer = 0;
					}
					
					float evaluateIndex = animationTimer/traceAnimation.animTime;//get teh % done for our animation
					float curveValue = traceAnimation.animCurve.Evaluate(evaluateIndex);// evaulate the curve at 'evaluateIndex'

					for(int i = 0; i < indicies.Count; i++)
					{
						//find the point we're supposed to be on
						int index = indicies[i] + (int)(verticies.Length*curveValue);
						if(index >= verticies.Length)
						{
							index -= verticies.Length;
						}

						//Debug.Log(currentVertex);
						//move to the correct location
						tracers[i].transform.position = transform.TransformPoint(verticies[index]);
					}
					
					animationTimer += Time.deltaTime;
				}
				yield return null;
			}
			
			this.GetComponent<MeshRenderer> ().enabled = true;
			EnableParticles (false);
		}
		
		//Particles ____________________________________________________
		void PoolParticles() // instantiate the required number of tracers.
		{
			pooledParticles = new List<GameObject> ();
			for (int i = 0; i < numTracers; i++) 
			{
				Vector3 vertex = verticies[Random.Range(0,verticies.Length-1)];
				Vector3 pos =transform.TransformPoint(vertex);
				GameObject newPS = Instantiate(particleSystemPrefab,pos,this.transform.rotation) as GameObject;
				newPS.hideFlags = HideFlags.HideInHierarchy;
				pooledParticles.Add(newPS);
				newPS.gameObject.SetActive(false);
			}
		}


		void EnableParticles(bool tf)
		{
			foreach(GameObject particle in pooledParticles)
			{
				particle.SetActive(tf);
			}
		}


	}

	[System.Serializable]
	public class TraceAnimation
	{
		public bool play = true;
		public AnimationCurve animCurve; // animate the line traveling from one verticy to teh next.
		public float animTime = 1; // the time it takes for 1 animation to complete

	}
}
