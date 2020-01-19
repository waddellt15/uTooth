using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MeshTracer
{
	public class VertexParticles : MonoBehaviour {
		
		public WaveAnimation waveAnimation; // parameters to control the animation
		public float particleLifetime = 1.0f; // lifetime of each particle
        public float emissionRate = 1; //x particles/second at each vertex location
		public GameObject particleSystemPrefab; // the particle system used to emit particles
		public bool onStart = true; // play the effect in the OnStart() method
		public bool hideMesh = true; // hide the mesh!
		public bool animatedMesh = false; // is this mesh animated? if so it will re-assign vertex locations during the animations
		public CameraCulling cameraCulling; // limit effects being played, based on the camera.
		/// <summary>
		/// the type of animation
		/// Static - emit a particle at each vertex, make them larger/smaller using waveAnimation curve
		/// Wave_Direction - emit particles in a wave across the mesh in the specified local direction. 
		/// 	- speed and other parameters are controlled by 'waveAnimation'
		/// </summary>
		public Type type = Type.STATIC; 
		public enum Type{STATIC, WAVE_RIGHT,WAVE_LEFT,WAVE_UP,WAVE_DOWN,WAVE_FORWARD,WAVE_BACK};
		public int numTracers = 1; // the number of waves

		private Type lastType;
		[Range(0,1)]
		private float waveEffectRange = .25f; // what % of the verticies, does the wave affect
		[Range(0,1)] // what % of verticies are used
		public float percentVerticies = 1f;
		public bool randStartPos = false; // start each wave at a random position

		private float emissionTime = 0;// particleTime = particleLifetime/particlesPerVertex. = time we should wait in between emitting perticles per vertex
		private bool playEffect = false; // are we playing the effect?
		private Mesh mesh; // the mesh
		private Vector3[] vertices; // mesh.verticies
		private List<int> someVerticies; // indicies into the verticies array, used when 'percentVerticies' is < 1
		private float[,,] emittedParticledBuffer; // track the particles that have been emitted. So we aren't constantly emitting more particles than necessary, at each vertex location.
		private float animationTimer = 0; // timer for the animation
		private ParticleSystem particleSystem; // instance of 'particleSystemPrefab'
		private Camera camera; //camera used for effect culling. Make this public if you want to set your own camera.
		private Plane[] planes; //camera frustrum planes
		private Bounds bounds; // this object's bounds. Set to Vector3.one for simplicity
		private bool objectVisible = true; //is this object currently visible?
		private Vector3 boundsSize = Vector3.one * .1f; // used as a bounding box for each vertex. 
		private Collider collider; // this object's collider component
		private SkinnedMeshRenderer skin; // mesh renderer for animated objects
		private Mesh bakedMesh; // baked mesh vertex values, for animated meshes
        ParticleSystem.EmitParams emitParams; // settings for particle emission

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

			vertices = mesh.vertices;
            //Debug.Log("Verts: " + vertices.Length);

            emissionTime = 1.0f / emissionRate;

			// grab 'percentVerticies' % of the verticies
			someVerticies = new List<int>();
			for(int i = 0; i < vertices.Length; i++)
			{
				if(Random.value <= percentVerticies)
				{
					someVerticies.Add(i);
				}
			}
            //Debug.Log("Someverts: " + someVerticies.Count);
            //particlebuffer[verticies,tracers,1]
            emittedParticledBuffer = new float[someVerticies.Count,Mathf.Max(1,numTracers),1]; 
			for(int i = 0; i < someVerticies.Count; i++)
			{
				for(int t = 0; t < numTracers; t++)
				{
					emittedParticledBuffer[i,t,0] = Random.value*emissionTime;
				}
			}

			//instantiate the prefab
            GameObject particleObj = Instantiate(particleSystemPrefab, this.transform.position, this.transform.rotation) as GameObject;
            particleObj.transform.SetParent(this.transform);
            particleSystem = particleObj.GetComponent<ParticleSystem>();
            particleSystem.playOnAwake = false;
            particleSystem.loop = false;
            particleSystem.simulationSpace = ParticleSystemSimulationSpace.World;// use world space
            particleSystem.maxParticles = (int)(someVerticies.Count*emissionRate);
            particleSystem.Stop();

            //play the effect!		
            if (onStart) 
			{
				PlayEffect ();
			}

			lastType = type;
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
				vertices = bakedMesh.vertices;

				OrderVerticies();
			}

			if(lastType != type)
			{
				PlayEffect(); // reset the effect
			}

			lastType = type;
		}

        bool PointOnCamera(Vector3 position)
		{
			bounds = new Bounds(position,boundsSize);//
			return GeometryUtility.TestPlanesAABB(planes, bounds); //planes updated in update loop
		}

		/// <summary>
		/// Plays the appropriate effect, based on Type.
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
			switch(type)
			{
			case Type.STATIC:
				StartCoroutine(SparkleVerticies());
				break;
			case Type.WAVE_RIGHT:
			case Type.WAVE_LEFT:
			case Type.WAVE_UP:
			case Type.WAVE_DOWN:
			case Type.WAVE_FORWARD:
			case Type.WAVE_BACK:
				StartCoroutine(Wave());
				break;
			default:
				StartCoroutine (SparkleVerticies ());
				break;
			}
		}

		public void OrderVerticies()
		{
			switch(type)
			{
			case Type.STATIC:
				break;
			case Type.WAVE_RIGHT:
				//compile a list of ordererd verticies, by x
				Vector3[] temp = vertices.OrderBy(vertex => vertex.x).ToArray();
				List<Vector3> tempVerticies = new List<Vector3>();
				for(int i = 0; i < temp.Length; i++)
				{
					tempVerticies.Add(temp[i]);
				}
				vertices = tempVerticies.ToArray();
				break;
			case Type.WAVE_LEFT:
				//compile a list of ordererd verticies, by -x
				temp = vertices.OrderBy(vertex => -vertex.x).ToArray();
				tempVerticies = new List<Vector3>();
				for(int i = 0; i < temp.Length; i++)
				{
					tempVerticies.Add(temp[i]);
				}
				vertices = tempVerticies.ToArray();
				break;
			case Type.WAVE_UP:
				//compile a list of ordererd verticies, by y
				temp = vertices.OrderBy(vertex => vertex.y).ToArray();
				tempVerticies = new List<Vector3>();
				for(int i = 0; i < temp.Length; i++)
				{
					tempVerticies.Add(temp[i]);
				}
				vertices = tempVerticies.ToArray();
				break;
			case Type.WAVE_DOWN:
				//compile a list of ordererd verticies, by -y
				temp = vertices.OrderBy(vertex => -vertex.y).ToArray();
				tempVerticies = new List<Vector3>();
				for(int i = 0; i < temp.Length; i++)
				{
					tempVerticies.Add(temp[i]);
				}
				vertices = tempVerticies.ToArray();
				break;
			case Type.WAVE_FORWARD:
				//compile a list of ordererd verticies, by z
				temp = vertices.OrderBy(vertex => vertex.z).ToArray();
				tempVerticies = new List<Vector3>();
				for(int i = 0; i < temp.Length; i++)
				{
					tempVerticies.Add(temp[i]);
				}
				vertices = tempVerticies.ToArray();
				break;
			case Type.WAVE_BACK:
				//compile a list of ordererd verticies, by -z
				temp = vertices.OrderBy(vertex => -vertex.z).ToArray();
				tempVerticies = new List<Vector3>();
				for(int i = 0; i < temp.Length; i++)
				{
					tempVerticies.Add(temp[i]);
				}
				vertices = tempVerticies.ToArray();
				break;
			default:
				break;
			}
		}
		
		//iEnumerators______________________________________________________

		//make all of the verticles larger or smaller based on the waveAnimation parameters
		IEnumerator SparkleVerticies()
		{

			playEffect = true;
			animationTimer = 0;
           
            

			while(playEffect)
			{
				//play the animation effect
	
				if(waveAnimation.play && objectVisible )
				{
					if(animationTimer > waveAnimation.animTime)
					{
						animationTimer -= waveAnimation.animTime;
					}
					
					float percent = animationTimer/waveAnimation.animTime; // % along our animation
					float sparkleValue = waveAnimation.animCurve.Evaluate(percent); // evaulate the animation curve at 'evaluateIndex'
					float particleSize = waveAnimation.minSize + (waveAnimation.maxSize - waveAnimation.minSize)*sparkleValue; // get the size of the marticle. Based on sparkleValue and min/max particle size.
					
					//Debug.Log("eval at " + evaluateIndex + " = " + sparkleValue + " size: " + particleSize);
					//emit all the particles at their appropriate size
					for(int i = 0; i < someVerticies.Count; i++)
					{
						//Vector3 pos = this.transform.position - transform.InverseTransformPoint(verticies[i]);
						int index = someVerticies[i];
						Vector3 pos = transform.TransformPoint(vertices[index]);
						if(cameraCulling.perVertex) // if per vertex culling is enabled
						{
							if(PointOnCamera(pos)) // if this vertex is on the camera
							{
								float delta = Time.time - emittedParticledBuffer[i,0,0]; // current time minus last time a particle was emitted
								if(delta >= emissionTime) 
								{
                                    Emit(pos, Vector3.zero, particleSize, particleLifetime, particleSystem.startColor);
									emittedParticledBuffer[i,0,0] = Time.time;
								}
							}
						}
						else //otherwise
						{
							//emit particles regardless
							float delta = Time.time - emittedParticledBuffer[i,0,0]; // current time minus last time a particle was emitted
							if(delta >= emissionTime) 
							{
                                Emit(pos, Vector3.zero, particleSize, particleLifetime, particleSystem.startColor);
                                emittedParticledBuffer[i,0,0] = Time.time;
							}
						}
						
					}
					
					animationTimer += Time.deltaTime;
				}
				yield return null;
			}
			
			//this.GetComponent<MeshRenderer> ().enabled = true;

		}
		
		// the wave animation, using the "verticies" array, which has previously been sorted in the PlayEffect() method	
		IEnumerator Wave()
		{
			playEffect = true;
			animationTimer = 0;

			//initialize our tracers
			List<int> indicies = new List<int> ();
			int range = (int)(vertices.Length * waveEffectRange);
			for(int i = 0; i < numTracers; i++)
			{
				int index = 0;
				if(randStartPos)
				{
					index = Random.Range(0,someVerticies.Count);
				}
				else if(i != 0) //otherwise, evenly space the waves
				{
					index = (int)(someVerticies.Count/(i+1));
				}
				indicies.Add(index); // start at a random place inside our array
			}

			while(playEffect)
			{
				//play the animation effect
				if(waveAnimation.play && objectVisible)
				{
					if(animationTimer > waveAnimation.animTime)
					{
						animationTimer = 0;
					}
					
					float evaluateIndex = animationTimer/waveAnimation.animTime; // % along the animation
					float sparkleValue = waveAnimation.animCurve.Evaluate(evaluateIndex); // evaluate the curve at that %

					//Debug.Log("eval at " + evaluateIndex + " = " + sparkleValue + " size: " + particleSize);
					for(int t = 0; t < numTracers; t++)
					{
						//eval index = startIndex + (howMuchWeSparkle*numVerticies)
						int evalIndex = indicies[t] + (int)(sparkleValue*someVerticies.Count);// teh evalaute curve(%) relative to our particleindicies
						if(evalIndex >= someVerticies.Count)
						{
							evalIndex -= someVerticies.Count;
						}
						//Debug.Log("index[" + t + "] = " + evalIndex);
						//Debug.Log("Emitting: " + verticies.Length + " particles, starting at " + evalIndex);

						//set the size for each particle
						for(int i = 0; i < someVerticies.Count; i++)
						{
							//we only want to resize particles within our 'wave range', otherwise we'll size other waves to 0, and teh last wave is the only one that works
							if(Mathf.Abs(i - evalIndex) <= range)
							{
								int index = someVerticies[i];
								//Debug.Log("index: " + evalIndex);
								float size = 0;
								if( i != evalIndex)
								{
									// size = 1/distance. that way particles that are 'close' to the eval curve are large, and vise versa
									size = waveAnimation.maxSize*(1.0f/Mathf.Abs(i-evalIndex));
									Vector3 pos = transform.TransformPoint(vertices[index]);
									if(cameraCulling.perVertex) // if per vertex culling is enabled
									{
										if(PointOnCamera(pos)) // if this vertex is on the camera
										{
											float delta = Time.time - emittedParticledBuffer[i,t,0]; // current time minus last time a particle was emitted
											if(delta >= emissionTime) 
											{
                                                Emit(pos, Vector3.zero, size, particleLifetime, particleSystem.startColor);
                                                emittedParticledBuffer[i,t,0] = Time.time;
											}
										}
									}
									else //otherwise
									{
										//emit particles regardless
										float delta = Time.time - emittedParticledBuffer[i,t,0]; // current time minus last time a particle was emitted
										if(delta >= emissionTime) 
										{
                                            Emit(pos, Vector3.zero, size, particleLifetime, particleSystem.startColor);
                                            emittedParticledBuffer[i, t, 0] = Time.time;
                                        }

									}
								}
							}
						}

					}
					
					animationTimer += Time.deltaTime;
				}
				yield return null;
			}
			
			//this.GetComponent<MeshRenderer> ().enabled = true;

		}


        void Emit(Vector3 pos, Vector3 velicotiy, float size, float lifeTime ,Color color)
        {
            emitParams.position = pos;
            emitParams.velocity = Vector3.zero;
            emitParams.startSize = size;
            emitParams.startLifetime = particleLifetime;
            emitParams.startColor = particleSystem.startColor;

            particleSystem.Emit(emitParams, 1);
            //ps.Emit(pos,Vector3.zero,size,particleLifetime,ps.startColor);
            
        }

	}

	[System.Serializable]
	public class WaveAnimation
	{
		public bool play = true; // play the animation
		public AnimationCurve animCurve; // animate the line traveling from one verticy to teh next.
		public float animTime = 1; // the time it takes for 1 animation to complete
		public float minSize = 0; // min size of the particle
		public float maxSize = 1; // max size of the particle
	}

	[System.Serializable]
	public class CameraCulling
	{
		public bool perObject = true; //if the object is out of scene, don't play effects on it
		public bool perVertex = true; //if the vertex is out of scene, don't play effects on it
	}
}
