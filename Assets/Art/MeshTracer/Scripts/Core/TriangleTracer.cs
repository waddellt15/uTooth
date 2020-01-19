using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace MeshTracer
{
	public class TriangleTracer : MonoBehaviour {

		public LineAnimation lineAnimation; // controls HOW the triangles are traced.
		public GameObject tracer;// Should have a trailrenderer attached. Moved along the triangles to trace them out. 
		public Type type = Type.ALL; // the type of TriangleTracer effect we want tot play
		[Range(0f,1f)] 
		public float percentTriangles = 1.0f; // what percent of the triangles will we use. //adjust this for large meshes.
		public enum Type{ALL,LOOP,TRACE_RANDOM,TRACE_CONNECTED,DISCO};
		/// <summary>
		/// All - traces all triangles simluatneously
		/// Loop - loop through all the triangles, tracing each one once.
		/// Trace_Random - same as loop, but in a random order
		/// Trace_Connected - same as loop, but goes from one triangle to a random connected triangle
		/// Disco - trace out 'numtracers' of triangles at once, then get new triangles and repeat every animation.
		/// </summary>
		public int numTracers; // the number of tracers, applies to everything but Type.All
		public bool onStart = true; // play the effect in the OnStart() method
		public bool hideMesh = true; // hide the mesh
		public bool animatedMesh = false; // is this mesh animated? if so it will re-assign vertex locations during the animations
		public CameraCulling cameraCulling; // limit effects being played, based on the camera.
		public bool randStartPos = false; // start the tracer in a random position
		public bool debug = false; // displays triangles being drawn in the scene view.

		private Type lastType;
		private bool playEffect = false; //is an effect playing?
		private Mesh mesh; // the mesh
		private Vector3[] verticies; // mesh.verticies
		private int[] triangles; // int indicies in the verticies array, the subset of triangles we're using based on 'percentTriangles'
		private int[] meshTriangles; // int indicies in the verticies array, all of the triangles in teh mesh
		private int numTriangles; // number of triangles in the mesh
		private List<GameObject> pooledParticles; // pooled 'tracers' or particles that get created OnStart()
		private float animationTimer = 0;
		private Camera camera; //camera used for effect culling. Make this public if you want to set your own camera.
		private Plane[] planes; //camera frustrum planes
		private Bounds bounds; // this object's bounds. Set to Vector3.one for simplicity
		private bool objectVisible = true; //is this object currently visible?
		private Vector3 boundsSize = Vector3.one * .1f; // used as a bounding box for each vertex. 
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
			meshTriangles = mesh.triangles;
			verticies = mesh.vertices;

			
			if(cameraCulling.perVertex) // if we have perVertex culling
			{
				if(tracer.GetComponent<TrailRenderer>()) // if our tracer has a trail renderer
				{
					if(tracer.GetComponent<TrailRendererHelper>() == null) // if the tracer doesn't have a trailRenderer helper
					{
						tracer.AddComponent<TrailRendererHelper>(); //add one, we need it because tracers will be enabled/disabled
					}
				}
			}

			lastType = type;

			PoolEffects();

			if(onStart)
			{
				PlayEffect();
			}
		}
		
		void Update()
		{
			//update planes, and bounds, for per-object culling
			if(cameraCulling.perObject || cameraCulling.perVertex)
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
			}
			
			//if per object culling
			if(cameraCulling.perObject)
			{
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
			}

			//if we've changed type, start the new effect
			if(lastType != type)
			{
				PoolEffects();
				PlayEffect();
			}

			lastType = type;
		}
		
		bool PointOnCamera(Vector3 position)
		{
			bounds = new Bounds(position,boundsSize);//
			return GeometryUtility.TestPlanesAABB(planes, bounds); //planes updated in update loop
		}

		// play the appropriate effect, based on Type
		public void PlayEffect()
		{
			//get that mesh out of here :P
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
			switch(type)
			{
			case Type.ALL:
				StartCoroutine (DrawAllTriangles ());
				break;
			case Type.LOOP:
				StartCoroutine(LoopTroughTriangles(meshTriangles));
				break;
			case Type.TRACE_RANDOM:
				//randomly order the triangles
				List<int> randomTris = new List<int>();
				List<int> remainingTris = new List<int>();
				remainingTris.AddRange(meshTriangles.ToList<int>());
				
				for(int i = 0; i < verticies.Length; i++)
				{
					int index = Random.Range(0,remainingTris.Count-1);
					randomTris.Add(remainingTris[index]);
					remainingTris.RemoveAt(index);
				}
				//run the loop method with randomly sorted input
				StartCoroutine(LoopTroughTriangles(randomTris.ToArray()));

			break;
			case Type.TRACE_CONNECTED:
				StartCoroutine(ConnectedTriangles());
				break;
			case Type.DISCO:
				StartCoroutine(Disco());
				break;
			default:
				StartCoroutine (DrawAllTriangles ());
				break;
			}
		}
		
		//iEnumerators______________________________________________________

		//draw every triangle
		IEnumerator DrawAllTriangles()
		{

			playEffect = true;
			EnableEffects (true);
			animationTimer = 0;
			
			while(playEffect)
			{
				//play the animation effect
				if(lineAnimation.play  && objectVisible)
				{
					//reset the animation every lineAnimation.animTime seconds
					if(animationTimer > lineAnimation.animTime)
					{
						animationTimer = 0;
					}
				
					float evaluateIndex = animationTimer/lineAnimation.animTime;//where we are in the current animation
					float curveVal = lineAnimation.animCurve.Evaluate(evaluateIndex); // the value of the animation curve evaluated at 'evaluateIndex'

					//loop through the triangles
					int particleIndex = 0;

					for(int i = 0 ; i < triangles.Length; i+=3)  // for each triangle
					{

						//indicies of vertex points, from teh trianglearray
						int index1 = triangles[i];
						int index2 = triangles[i+1];
						int index3 = triangles[i+2];


						//get the vericies of this triangle
						Vector3 a = transform.TransformPoint(verticies[index1]);
						Vector3 b = transform.TransformPoint(verticies[index2]);
						Vector3 c = transform.TransformPoint(verticies[index3]);
						GameObject particle = pooledParticles[particleIndex]; // grab a particle (tracer) from our pooled list
						particleIndex++; // go to the next particle
						if(cameraCulling.perVertex) // if per vertex culling is enabled
						{
							if(PointOnCamera(a)) // if this vertex is on the camera
							{
								//Move particles along each triangle
								particle.SetActive(true); //set it active
							}
							else
							{
								particle.SetActive(false); //de-activate
							}
						}
						else //otherwise
						{
							//Move particles along each triangle
							particle.SetActive(true); //set it active
						}

						if(particle.activeSelf)
						{
							if(debug)
							{
								Debug.DrawLine(a,b,Color.red);
								Debug.DrawLine(b,c,Color.green);
								Debug.DrawLine(c,a,Color.blue);
							}
							particle.transform.position = GetPositionOnTriangle(a,b,c,curveVal); // set it's position at the correct point along the triangle
						}

					}
					animationTimer += Time.deltaTime; // increment the animation timer
				}
				yield return null;
			}

			EnableEffects (false); // diable the effects
		}

		// draw each triangle 1 by 1, in the order in which they're passed in.
		IEnumerator LoopTroughTriangles(int[] sortedTriangles)
		{
			playEffect = true;
			EnableEffects (true);
			animationTimer = 0;
			GameObject particle = pooledParticles[0];// we only need 1 particle for this one, so we'll just grab the 1st pooled particle
			particle.SetActive(true);

			float timePerTriangle = lineAnimation.animTime/numTriangles; // time spent tracing each triangle
			int currentTriangle = 0; // triangle we're currently tracing
			int lastTriangle = 0; // the last triangle we traced

			while(playEffect)
			{
				//play the animation effect
				if(lineAnimation.play && objectVisible)
				{
					int particleIndex = 0;
					if(animationTimer > lineAnimation.animTime)
					{
						animationTimer = 0;
					}
								
					// the current triangle we're on!
					currentTriangle =(int)(animationTimer/timePerTriangle);
					int i = currentTriangle*3; //*3 because there are 3 vertices per triangle

					if( i < (sortedTriangles.Length - 3))
					{
						//indicies of vertex points, from teh trianglearray
						int index1 = sortedTriangles[i];
						int index2 = sortedTriangles[i+1];
						int index3 = sortedTriangles[i+2];
						
						//get the vericies of this triangle
						Vector3 a = transform.TransformPoint(verticies[index1]);
						Vector3 b = transform.TransformPoint(verticies[index2]);
						Vector3 c = transform.TransformPoint(verticies[index3]);

						if(debug)
						{
							Debug.DrawLine(a,b,Color.red);
							Debug.DrawLine(b,c,Color.green);
							Debug.DrawLine(c,a,Color.blue);
						}

						float trianglePercent = (animationTimer/timePerTriangle) - currentTriangle; // the percent of distance we've travelled along the triangle
				
						particle.transform.position = GetPositionOnTriangle(a,b,c,trianglePercent);
						lastTriangle = currentTriangle;
					}
					animationTimer += Time.deltaTime;
				}
				yield return null;
			}

			EnableEffects (false);
		}

		// trace a triangle, then trace a connected triangle, repeat.
		IEnumerator ConnectedTriangles()
		{
			playEffect = true;
			animationTimer = 0;

			float timePerTriangle = lineAnimation.animTime/numTriangles;
			int lastTriangle = 0;

			int counter = 1;
			float triangleTimer = 0; //timer for running animation on each triangle
			List<GameObject> tracers = new List<GameObject> ();
			List<int> indicies = new List<int> ();
			//compile our list of tracers, using pooled objects
			for(int i = 0; i < numTracers; i++)
			{
				GameObject tracer = pooledParticles [i];
				tracer.SetActive (true);
				tracer.hideFlags = HideFlags.HideInHierarchy;
				tracers.Add(tracer);
				int index = 0;
				if(randStartPos)// start at a random place inside our array
				{
					index = Random.Range (0, numTriangles-1) * 3;
				}
				indicies.Add(index); 
			}
			
			while (playEffect) {
				//play the animation effect
				if (lineAnimation.play && objectVisible) {
					for (int i = 0; i < numTracers; i++) 
					{

						float evaluateIndex = animationTimer / lineAnimation.animTime; // get the evaluation point for our animation curve
						float curveVal = lineAnimation.animCurve.Evaluate (evaluateIndex); // evaluate the curve at 'evaluateIndex'

						float triangleTime = curveVal * timePerTriangle;// the time to spend on this triangle

						//loop the animation
						if (animationTimer > lineAnimation.animTime) 
						{
							animationTimer = 0;
						}

						//once triangleTimer > triangleTime, go to the next triangle.
						if (triangleTimer > triangleTime) {
							int i1 = meshTriangles [indicies[i]];
							int i2 = meshTriangles [indicies[i] + 1];
							int i3 = meshTriangles [indicies[i] + 2];
							indicies[i] = GetTriangleWithSharedVertex (new int[]{i1,i2,i3}); // get index with a shared vertex
							triangleTimer = 0;
							counter++;
						}

						if (indicies[i] < (meshTriangles.Length - 3)) 
						{
							//indicies of vertex points, from teh trianglearray
							int index1 = meshTriangles [indicies[i]];
							int index2 = meshTriangles [indicies[i] + 1];
							int index3 = meshTriangles [indicies[i] + 2];
						
							//get the vericies of this triangle
							Vector3 a = transform.TransformPoint (verticies [index1]);
							Vector3 b = transform.TransformPoint (verticies [index2]);
							Vector3 c = transform.TransformPoint (verticies [index3]);
						
							if (debug) {
								Debug.DrawLine (a, b, Color.red);
								Debug.DrawLine (b, c, Color.green);
								Debug.DrawLine (c, a, Color.blue);
							}
						
							float trianglePercent = (triangleTimer / timePerTriangle);
						
							tracers[i].transform.position = GetPositionOnTriangle (a, b, c, trianglePercent); // draw at the correct position along the current triangle
							lastTriangle = indicies[i];
						}
						float time = Time.deltaTime;
						animationTimer += time;
						triangleTimer += time;
					}

				}
				yield return null;
			}

			EnableEffects (false);
		}

		/// <summary>
		/// display 'numTracers' triangles at once, then switch to new triangles after animTime
		/// </summary>
		IEnumerator Disco()
		{

			playEffect = true;
			EnableEffects (false);
			animationTimer = 0;

			//initialize our tracers
			int[] tracers = new int[numTracers];
			for(int i = 0 ; i < numTracers; i++)
			{
				tracers[i] = Random.Range(0,numTriangles-1);
			}
			int counter = 0;

			while(playEffect)
			{
				//play the animation effect
				if(lineAnimation.play && objectVisible)
				{
					//loop the animation
					if(animationTimer > lineAnimation.animTime)
					{
						animationTimer = 0;
						//get new tracers every 3rd loop
						if(counter == 3)
						{
							for(int i = 0 ; i < numTracers; i++)
							{
								pooledParticles[i].SetActive(false);
								tracers[i] = Random.Range(0,numTriangles-1);
							}
							counter = 0;
						}
						counter ++;
					}
					
					float evaluateIndex = animationTimer/lineAnimation.animTime; // what % into the animation are we
					float curveVal = lineAnimation.animCurve.Evaluate(evaluateIndex); // evaluate the curve at that %
					//loop through the triangles
					//int particleIndex = 0;
					for(int i = 0 ; i < numTracers; i++) 
					{
						int triangleIndex = tracers[i]*3;
						//indicies of vertex points, from teh trianglearray
						int index1 = meshTriangles[triangleIndex];
						int index2 = meshTriangles[triangleIndex+1];
						int index3 = meshTriangles[triangleIndex+2];
						
						//get the vericies of this triangle
						Vector3 a = transform.TransformPoint(verticies[index1]);
						Vector3 b = transform.TransformPoint(verticies[index2]);
						Vector3 c = transform.TransformPoint(verticies[index3]);

						//Move particles along each triangle
						GameObject particle = pooledParticles[i];
						if(cameraCulling.perVertex) // if per vertex culling is enabled
						{
							if(PointOnCamera(a)) // if this vertex is on the camera
							{
								//Move particles along each triangle
								particle.SetActive(true); //set it active
							}
							else
							{
								particle.SetActive(false); //de-activate
							}
						}
						else //otherwise
						{
							//Move particles along each triangle
							particle.SetActive(true); //set it active
						}
						

						if(particle.activeSelf)
						{
							particle.transform.position = GetPositionOnTriangle(a,b,c,curveVal); // position the particle (tracer) 
						}
						if(debug)
						{
							Debug.DrawLine(a,b,Color.red);
							Debug.DrawLine(b,c,Color.green);
							Debug.DrawLine(c,a,Color.blue);
						}
						
					}
					animationTimer += Time.deltaTime;
				}
				yield return null;
			}
			

			EnableEffects (false);

		}

		//returns the index into the vertex array for a triangle, i.e. if we return index 6, then the triangle is at index 6,7,8
		int GetTriangleWithSharedVertex(int[] triangle)
		{
			int a = triangle[0];
			int b = triangle[1];
			int c = triangle[2];


			// compile a list of triangles connected with this one
			List<int> possibleTriangles = new List<int> (); 
			for(int i = 0; i < meshTriangles.Length; i+=3)
			{
				if(!possibleTriangles.Contains(i))
				{
					bool matchA = (meshTriangles[i] == a || meshTriangles[i+1] == a || meshTriangles [i+2] == a);
					bool matchB = (meshTriangles[i] == b || meshTriangles[i+1] == b || meshTriangles [i+2] == b);
					bool matchC = (meshTriangles[i] == c || meshTriangles[i+1] == c || meshTriangles [i+2] == c);
					if(matchA || matchB || matchC)
					{
						possibleTriangles.Add(i);

					}
				}

			}

			int returnIndex = possibleTriangles [Random.Range (0, possibleTriangles.Count - 1)]; // get a random triangle out of our list of possible connected triangles

			if(possibleTriangles.Count > 0)
			{
				return returnIndex;
			}

			return 0;
		}

		/// <summary>
		/// Gets the position on triangle.
		/// </summary>
		/// <returns>The position on triangle.</returns>
		/// <param name="a">The alpha component.</param>
		/// <param name="b">The blue component.</param>
		/// <param name="c">C.</param>
		/// <param name="percent">Percent.</param> denotes what percent of the distance we've travelled. ex: in a perfect triangle, each vertex is 1/3rd of teh distance
		Vector3 GetPositionOnTriangle(Vector3 a, Vector3 b, Vector3 c, float percent)
		{
			Vector3 ab = b - a;
			Vector3 bc = c - b;
			Vector3 ca = a - c; 
			float triangleLength = ab.magnitude + bc.magnitude + ca.magnitude; // length of all 3 edges
			float distance = triangleLength*percent; // the % we've travelled along our triangle

			//get our position along the triangle
			//figure out which leg of the triangle it's on
			//position = Vertex + toNext*(distanceAlongLeg)
			if(distance < ab.magnitude) // edge1
			{
				return a + ab*(distance/ab.magnitude); 
			}
			else if( distance < (ab.magnitude + bc.magnitude)) //edge 2
			{
				return b + bc*((distance-ab.magnitude)/bc.magnitude); 
			}
			else // edge 3
			{
				return c + ca*((distance-ab.magnitude - bc.magnitude)/ca.magnitude);
			}

		}
		
		//Particles ____________________________________________________
		//pool the particles/tracers and triangles used for tracing
		void PoolEffects()
		{
			pooledParticles = new List<GameObject> ();
			List<int> randomTriangles = new List<int>();


			int particlesToPool;
			if(type == Type.ALL)
			{
				//use all of the triangles, pool 1 particle for each triangle
				particlesToPool = meshTriangles.Length/3;
			}
			else
			{
				//use min(allTriangles,numTracers)
				particlesToPool = Mathf.Min(meshTriangles.Length/3,numTracers);

			}

			//create 1 particle per triangles
			for(int i = 0 ; i < particlesToPool; i++)
			{
				//factor in 'percentTriangles'
				if(Random.value < percentTriangles)
				{
					int index = i*3;// we'll use this triangle.
					randomTriangles.Add(meshTriangles[index]); //triangle vertexA 
					randomTriangles.Add(meshTriangles[index+1]); //triangle vertexB 
					randomTriangles.Add(meshTriangles[index+2]); //triangle vertexC 
					GameObject newPS = Instantiate(tracer,this.transform.position,this.transform.rotation) as GameObject;
					newPS.hideFlags = HideFlags.HideInHierarchy;
					pooledParticles.Add(newPS);
					newPS.gameObject.SetActive(false);
				}
			}
			numTracers = pooledParticles.Count; //update numTracers in case allTriangles < numTracers
			triangles = randomTriangles.ToArray(); // compile our final list of triangles, a random subset of all the triangles, based on 'percentTriangles';
			numTriangles = meshTriangles.Length / 3;
		}

		//enable/diable the pooled objects
		void EnableEffects(bool tf)
		{
			for(int i = 0 ; i < pooledParticles.Count; i++)
			{
				pooledParticles[i].SetActive(tf);
			}
		}
		
	}

	[System.Serializable]
	public class LineAnimation
	{
		public bool play = true; // play the animation
		public AnimationCurve animCurve; // animate the line traveling from one verticy to teh next.
		public float animTime = 1; // the time it takes for 1 animation to complete
	}
}
