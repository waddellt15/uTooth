using UnityEngine;
using UnityEngine.Events;
//using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace MeshTracer
{
	[RequireComponent(typeof(MeshRenderer))]
	public class EdgeTracer : MonoBehaviour {
		
		public List<LineTrace> lineTracers; //list of line tracers, each one will replace every edge with a line.
		public bool useOutline = true; // use the edges outline the mesh, instead of all mesh edges.
		public bool onStart = true; // create the edges in the OnStart() method.
		public bool instantDraw = false; // if this is true, everything will be drawn instantly, instead of animated using LineTrace.drawTime
		public bool hideMesh = false; // hide the original mesh if this is true
		public UnityEvent OnFinish; //set up public methods to be called when the animation is finished
		
		private Edge[] edges; // array of all edges
		private Mesh mesh; // the mesh, pulled in from the meshRenderer scrip attached to this object
		private GameObject[,] tracerMatrix; // matrix of instantiated lines. row indicates which tracer prefab. column indicates which instance
		private Vector3[] verticies; // verticies of the mesh.
		private int[] triangles; // int indicies into the verticies array, see the Unity API for mesh.triangles for more info
		private LineTrace lastTracer; // the last tracer to finish it's draw animation. After this tracer (line) is done drawing, we invoke the "OnFinish()" event(s);

		// Use this for initialization
		void Start () 
		{

			mesh = this.GetComponent<MeshFilter> ().mesh;
			if(hideMesh)
			{
				this.GetComponent<MeshRenderer>().enabled = false;
			}
			verticies = mesh.vertices;
			triangles = mesh.triangles;
			if(useOutline)
			{
				edges = BuildManifoldEdges(mesh);
			}
			else{
				edges = GetAllEdges(mesh);
			}
			FillTracerMatrix();

			//get the last linetracer
			if(lineTracers.Count > 0) //initialize to the first tracer
			{
				lastTracer = lineTracers[0];
			}
			//find the line tracer with the latest end time
			float endTime = 0;
			foreach(LineTrace tracer in lineTracers)
			{
				if(tracer.endTime > endTime)
				{
					endTime = tracer.endTime;
					lastTracer = tracer;
				}
			}

			//if the onstart option is selected
			if(onStart)
			{
				//insta draw or animated draw
				if(instantDraw) 
				{
					InstantDraw();
				}
				else{
					AnimatedDraw();
				}
			}
		}

		/// <summary>
		/// Fills the tracer matrix.
		/// A matrix of instantiated lines. row indicates which tracer prefab. column indicates which instance
		/// </summary>
		void FillTracerMatrix()
		{
			tracerMatrix = new GameObject[lineTracers.Count,edges.Length];
				//new LineTrace[lineTracers.Count,edges.Length];
			for(int r =0; r < lineTracers.Count; r++)
			{
				//create an empty gameobjec to hold all the lines
				GameObject lineParentObject = new GameObject();
				lineParentObject.transform.position = this.transform.position;
				lineParentObject.transform.rotation = this.transform.rotation;
				lineParentObject.name = lineTracers[r].name;
				lineParentObject.transform.parent = this.transform;

				for(int c = 0; c < edges.Length; c++)
				{
					int index = r*edges.Length + c;
					//Debug.Log("Creating tracer: " + index);
					Edge edge = edges[c];
					Vector3 pos;
					//position the line tracers
					if(lineTracers[r].drawPoint != null)
					{
						pos = lineTracers[r].drawPoint.position;
					}
					else
					{
						pos = this.transform.position;
						//pos = transform.TransformPoint(verticies[edge.vertexIndex[0]]);
					}
					GameObject tracer = NewLine(lineTracers[r],lineParentObject.transform,pos);//new LineTrace(lineTracers[r],this.transform);
					tracerMatrix[r,c] = tracer;
				}
			}
		}

		/// <summary>
		/// Instantantly draw all of the lines.
		/// </summary>
		public void InstantDraw()
		{
			//Debug.Log("Drawing: " + edges.Length + " lines");
			for(int r =0; r < lineTracers.Count; r++)
			{
				for(int c = 0; c < edges.Length; c++)
				{
					int index = r*edges.Length + c;
					LineTrace tracer = lineTracers[r];
					GameObject lineObj = tracerMatrix[r,c];
					LineRenderer line = lineObj.GetComponent<LineRenderer>();

					Edge edge = edges[c];
					//get the points in space we need
					Vector3 a = transform.TransformPoint(verticies[edge.vertexIndex[0]]);
					Vector3 b = transform.TransformPoint(verticies[edge.vertexIndex[1]]);
					Vector3 aToB = b - a;
					Vector3 offsetDir = GetNormalDirection(edge);

					//Debug.Log("Drawing tracer: " + index + " from " + a + " to " + b + " vec: " + aToB) ;
					//place all of the points on the line
					for( int seg = 0; seg < tracer.lineSegments; seg++)
					{
						float percent = (float)seg/tracer.lineSegments;
						Vector3 offset = offsetDir*tracer.curve.Evaluate(percent)*tracer.amplitude;
						//Debug.Log("%: " + percent);
						Vector3 pos = a + aToB*percent + offset;
						line.SetPosition(seg,pos);
					}
				}
			}

			if(OnFinish != null)
			{
				OnFinish.Invoke();
			}

		}

		/// <summary>
		/// Draw all of the lines, using their startTime/endTime
		/// </summary>
		public void AnimatedDraw()
		{
			StopAllCoroutines ();
			foreach(LineTrace tracer in lineTracers)
			{
				if(tracer.endTime < tracer.startTime)
				{
					tracer.endTime = tracer.startTime;
				}
				//start drawing right away
				if(tracer.startTime == 0)
				{
					StartCoroutine (AnimateDrawing (tracer));
				}
				else // or wait 'startTime' if it's > 0
				{
					StartCoroutine (delayedStart (tracer));
				}
			}
		}

		//similar to Invoke but using the tracer parameter
		IEnumerator delayedStart(LineTrace tracer)
		{
			float timer = 0;

			while(timer < tracer.startTime)
			{

				timer += Time.deltaTime;
				yield return null;
			}
			StartCoroutine (AnimateDrawing (tracer));
		}

		/// <summary>
		/// Animates the drawing.
		/// Draw the line, starting at startTime and ending at endTime.
		/// </summary>
		/// <returns>The drawing.</returns>
		/// <param name="tracer">Tracer.</param>
		IEnumerator AnimateDrawing(LineTrace tracer)
		{
			float timer = 0;
			float stepTimer = 0f;
			float stepSize = tracer.endTime / tracer.lineSegments;
			int index = 0;

			//Debug.Log("Drawing: " + edges.Length + " lines");
			while(timer <= tracer.endTime || index < tracer.lineSegments)
			{
				if(stepTimer >= stepSize)
				{
					//Debug.Log("Drawing index: " + index);

					for(int c = 0; c < edges.Length; c++)
					{
						int row = lineTracers.IndexOf(tracer);
						GameObject lineObj = tracerMatrix[row,c];
						LineRenderer line = lineObj.GetComponent<LineRenderer>();
						
						Edge edge = edges[c];
						//get the points in space we need
						Vector3 a = transform.TransformPoint(verticies[edge.vertexIndex[0]]);
						Vector3 b = transform.TransformPoint(verticies[edge.vertexIndex[1]]);
						Vector3 aToB = b - a;
						Vector3 offsetDir = GetNormalDirection(edge);
						
						//Debug.Log("Drawing tracer: " + index + " from " + a + " to " + b + " vec: " + aToB) ;
						float percent = (float)index/tracer.lineSegments;
						Vector3 offset = offsetDir*tracer.curve.Evaluate(percent)*tracer.amplitude;
						//Debug.Log("%: " + percent);
						Vector3 pos = a + aToB*percent + offset;
						line.SetPosition(index,pos);
					}

					index++;
					stepTimer = 0;
				}
				
				float delta = Time.deltaTime;
				timer += delta;
				stepTimer += delta;
				yield return null;
			}

			if(tracer == lastTracer)
			{
				if(OnFinish != null)
				{
					OnFinish.Invoke();
				}
			}
			//Debug.Log ("done drawing edges");
		}

		//Get the normal direction for the edge, by using the cross product of 2 edges on the corresponding triangle.
		public Vector3 GetNormalDirection(Edge edge)
		{
			int i = edge.faceIndex[0]*3;//i = triangleIndex

			//get index points
			int index1 = triangles[i];
			int index2 = triangles[i+1];
			int index3 = triangles[i+2];

			//get the vericies of this triangle
			Vector3 a = transform.TransformPoint(verticies[index1]);
			Vector3 b = transform.TransformPoint(verticies[index2]);
			Vector3 c = transform.TransformPoint(verticies[index3]);

			Vector3 aToB = b - a;
			Vector3 bToC = c - b;

			return Vector3.Cross(aToB,bToC).normalized;
		}

		//get all the edges on the mesh.
		public Edge[] GetAllEdges(Mesh mesh)
		{
			int[] triangles = mesh.triangles;
			int numTriangles = triangles.Length/3;
			Edge[] edges = new Edge[triangles.Length];

			int triangleIndex = 0;
			for(int i = 0; i < triangles.Length; i+=3)
			{
				Edge e1 = new Edge();
				e1.vertexIndex = new int[2]{triangles[i],triangles[i+1]};
				e1.faceIndex = new int[2]{triangleIndex,triangleIndex};
				Edge e2 = new Edge();
				e2.vertexIndex = new int[2]{triangles[i+1],triangles[i+2]};
				e2.faceIndex = new int[2]{triangleIndex,triangleIndex};
				Edge e3 = new Edge();
				e3.vertexIndex = new int[2]{triangles[i+2],triangles[i]};
				e3.faceIndex = new int[2]{triangleIndex,triangleIndex};
				edges[i] = e1;
				edges[i+1] = e2;
				edges[i+2] = e3;
				triangleIndex++;
			}
			return edges;
		}

		// Get the edges that outline the mesh.
		public static Edge[] BuildManifoldEdges(Mesh mesh)
		{
			// Build a edge list for all unique edges in the mesh
			Edge[] edges = BuildEdges(mesh.vertexCount, mesh.triangles);
			
			// We only want edges that connect to a single triangle
			ArrayList culledEdges = new ArrayList();
			foreach (Edge edge in edges)
			{
				if (edge.faceIndex[0] == edge.faceIndex[1])
				{
					culledEdges.Add(edge);
				}
			}
			
			return culledEdges.ToArray(typeof(Edge)) as Edge[];
		}
		
		/// Builds an array of unique edges
		/// This requires that your mesh has all vertices welded. However on import, Unity has to split
		/// vertices at uv seams and normal seams. Thus for a mesh with seams in your mesh you
		/// will get two edges adjoining one triangle.
		/// Often this is not a problem but you can fix it by welding vertices 
		/// and passing in the triangle array of the welded vertices.
		public static Edge[] BuildEdges(int vertexCount, int[] triangleArray)
		{
			int maxEdgeCount = triangleArray.Length;
			int[] firstEdge = new int[vertexCount + maxEdgeCount];
			int nextEdge = vertexCount;
			int triangleCount = triangleArray.Length / 3;
			
			for (int a = 0; a < vertexCount; a++)
				firstEdge[a] = -1;
			
			// First pass over all triangles. This finds all the edges satisfying the
			// condition that the first vertex index is less than the second vertex index
			// when the direction from the first vertex to the second vertex represents
			// a counterclockwise winding around the triangle to which the edge belongs.
			// For each edge found, the edge index is stored in a linked list of edges
			// belonging to the lower-numbered vertex index i. This allows us to quickly
			// find an edge in the second pass whose higher-numbered vertex index is i.
			Edge[] edgeArray = new Edge[maxEdgeCount];
			
			int edgeCount = 0;
			for (int a = 0; a < triangleCount; a++)
			{
				int i1 = triangleArray[a * 3 + 2];
				for (int b = 0; b < 3; b++)
				{
					int i2 = triangleArray[a * 3 + b];
					if (i1 < i2)
					{
						Edge newEdge = new Edge();
						newEdge.vertexIndex[0] = i1;
						newEdge.vertexIndex[1] = i2;
						newEdge.faceIndex[0] = a;
						newEdge.faceIndex[1] = a;
						edgeArray[edgeCount] = newEdge;
						
						int edgeIndex = firstEdge[i1];
						if (edgeIndex == -1)
						{
							firstEdge[i1] = edgeCount;
						}
						else
						{
							while (true)
							{
								int index = firstEdge[nextEdge + edgeIndex];
								if (index == -1)
								{
									firstEdge[nextEdge + edgeIndex] = edgeCount;
									break;
								}
								
								edgeIndex = index;
							}
						}
						
						firstEdge[nextEdge + edgeCount] = -1;
						edgeCount++;
					}
					
					i1 = i2;
				}
			}
			
			// Second pass over all triangles. This finds all the edges satisfying the
			// condition that the first vertex index is greater than the second vertex index
			// when the direction from the first vertex to the second vertex represents
			// a counterclockwise winding around the triangle to which the edge belongs.
			// For each of these edges, the same edge should have already been found in
			// the first pass for a different triangle. Of course we might have edges with only one triangle
			// in that case we just add the edge here
			// So we search the list of edges
			// for the higher-numbered vertex index for the matching edge and fill in the
			// second triangle index. The maximum number of comparisons in this search for
			// any vertex is the number of edges having that vertex as an endpoint.
			
			for (int a = 0; a < triangleCount; a++)
			{
				int i1 = triangleArray[a * 3 + 2];
				for (int b = 0; b < 3; b++)
				{
					int i2 = triangleArray[a * 3 + b];
					if (i1 > i2)
					{
						bool foundEdge = false;
						for (int edgeIndex = firstEdge[i2]; edgeIndex != -1; edgeIndex = firstEdge[nextEdge + edgeIndex])
						{
							Edge edge = edgeArray[edgeIndex];
							if ((edge.vertexIndex[1] == i1) && (edge.faceIndex[0] == edge.faceIndex[1]))
							{
								edgeArray[edgeIndex].faceIndex[1] = a;
								foundEdge = true;
								break;
							}
						}
						
						if (!foundEdge)
						{
							Edge newEdge = new Edge();
							newEdge.vertexIndex[0] = i1;
							newEdge.vertexIndex[1] = i2;
							newEdge.faceIndex[0] = a;
							newEdge.faceIndex[1] = a;
							edgeArray[edgeCount] = newEdge;
							edgeCount++;
						}
					}
					
					i1 = i2;
				}
			}
			
			Edge[] compactedEdges = new Edge[edgeCount];
			for (int e = 0; e < edgeCount; e++)
				compactedEdges[e] = edgeArray[e];
			
			return compactedEdges;
		}

		//create a new linerenderer
		GameObject NewLine(LineTrace tracer, Transform parent, Vector3 position)
		{	
			GameObject go = new GameObject(); 
			go.name = "line";
			//go.transform.position = position;
			go.transform.SetParent(parent);

			//go.hideFlags = HideFlags.HideInHierarchy;
			LineRenderer line = go.AddComponent<LineRenderer>();
			line.useWorldSpace = false;
			//lineMat = new Material(Shader.Find("Particles/Additive"));
			//Debug.Log("Creating line with material: " + tracer.lineMat);
			line.material = tracer.lineMat;
			line.SetColors(tracer.startColor, tracer.endColor);
			line.SetWidth(tracer.startWidth, tracer.endWidth);
			line.SetVertexCount(tracer.lineSegments);

			for( int i = 0; i < tracer.lineSegments; i++)
			{
				line.SetPosition(i,position);
			}
			
			return go;
		}

	}

	[System.Serializable]
	public class Edge
	{
		// The indiex to each vertex
		public int[] vertexIndex = new int[2];
		// The index into the face.
		// (faceindex[0] == faceindex[1] means the edge connects to only one triangle)
		public int[] faceIndex = new int[2];	
	}

	[System.Serializable]
	public class LineTrace
	{
		public string name = "line"; // name of the line
		public Transform drawPoint; // origin of laser that draws this line tracer! If this is null, it draws them from the starting vertex
		public float startTime = 0f; // start time of animation draw
		public float endTime = 5.0f; // end time of animation draw
		public AnimationCurve curve; // shape of the line.
		public float amplitude; // height of the line, multiplied by 'curve' when drawing each edge.
		public Material lineMat; // material for each LineRenderer
		public Color startColor; // start color of the LineRendreer
		public Color endColor; // end color of the LineRenderer
		public float startWidth; // start width of the LineRenderer
		public float endWidth; // end width of the LineRenderer
		public int lineSegments; // num segments in the lineRenderer
		LineRenderer line; // the line itself.

		//constructor
		public LineTrace(LineTrace prefab, Transform parent)
		{

			name = prefab.name;
			curve = prefab.curve;
			startColor = prefab.startColor;
			endColor = prefab.endColor;
			startWidth = prefab.startWidth;
			endWidth = prefab.endWidth;
			lineSegments = prefab.lineSegments;
			amplitude = prefab.amplitude;
			//line = NewLine(prefab, parent);
		}
		
		public void SetLine(LineRenderer l)
		{
			line = l;
		}
		public LineRenderer Line()
		{
			return line;
		}
	}
}


