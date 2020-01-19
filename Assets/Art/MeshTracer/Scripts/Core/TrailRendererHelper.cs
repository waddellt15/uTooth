using UnityEngine;
using System.Collections;

namespace MeshTracer
{
	/// <summary>
	/// This helper class handles the issue where trails persist though disable-move-enable logic.
	/// it was found here: http://forum.unity3d.com/threads/trailrenderer-reset.38927/ 
	/// </summary>
	public class TrailRendererHelper : MonoBehaviour
	{
		protected TrailRenderer mTrail;
		protected float mTime = 0;
		
		void Awake()
		{
			mTrail = gameObject.GetComponent<TrailRenderer>();
			if (null == mTrail)
			{
				Debug.LogError("[TrailRendererHelper.Awake] invalid TrailRenderer.");
				return;
			}
			
			mTime = mTrail.time;
		}
		
		void OnEnable()
		{
			if (null == mTrail)
			{
				return;
			}
			
			StartCoroutine(ResetTrails());
		}
		
		IEnumerator ResetTrails()
		{
			mTrail.time = 0;
			
			yield return new WaitForEndOfFrame();
			
			mTrail.time = mTime;
		}
	}
}
