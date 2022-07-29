using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This component raycasts from the current Transform position, and fires hit events when the rays hit something.</summary>
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dHitExplosion")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Hit/Hit Explosion")]
	public class P3dHitExplosion : MonoBehaviour
	{
		public enum OrientationType
		{
			WorldUp,
			CameraUp
		}

		public enum NormalType
		{
			HitNormal,
			RayDirection
		}

		/// <summary>The radius of the explosion in world space.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [SerializeField] private float radius = 1.0f;

		/// <summary>The amount of rays that are cast.</summary>
		public int Count { set { count = value; } get { return count; } } [SerializeField] private int count = 20;

		/// <summary>The maximum delay between explosion and impact in seconds.</summary>
		public float DelayMax { set { delayMax = value; } get { return delayMax; } } [SerializeField] private float delayMax = 0.25f;

		/// <summary>The layers you want the raycast to hit.</summary>
		public LayerMask Layers { set { layers = value; } get { return layers; } } [SerializeField] private LayerMask layers = Physics.DefaultRaycastLayers;

		/// <summary>How should the hit point be oriented?
		/// WorldUp = It will be rotated to the normal, where the up vector is world up.
		/// CameraUp = It will be rotated to the normal, where the up vector is world up.</summary>
		public OrientationType Orientation { set { orientation = value; } get { return orientation; } } [SerializeField] private OrientationType orientation;

		/// <summary>Orient to a specific camera?
		/// None = MainCamera.</summary>
		public Camera Camera { set { _camera = value; } get { return _camera; } } [SerializeField] private Camera _camera;

		/// <summary>Which normal should the hit point rotation be based on?</summary>
		public NormalType Normal { set { normal = value; } get { return normal; } } [SerializeField] private NormalType normal;

		/// <summary>If you want the raycast hit point to be offset from the surface a bit, this allows you to set by how much in world space.</summary>
		public float Offset { set { offset = value; } get { return offset; } } [SerializeField] private float offset;

		/// <summary>Should the applied paint be applied as a preview?</summary>
		public bool Preview { set { preview = value; } get { return preview; } } [SerializeField] private bool preview;

		/// <summary>This allows you to override the order this paint gets applied to the object during the current frame.</summary>
		public int Priority { set { priority = value; } get { return priority; } } [SerializeField] private int priority;

		[System.NonSerialized]
		private P3dHitCache hitCache = new P3dHitCache();

		public P3dHitCache HitCache
		{
			get
			{
				return hitCache;
			}
		}

		[ContextMenu("Clear Hit Cache")]
		public void ClearHitCache()
		{
			hitCache.Clear();
		}

		protected virtual void Start()
		{
			ExplodeNow();
		}

		[ContextMenu("Explode Now")]
		public void ExplodeNow()
		{
			for (var i = 0; i < Count; i++)
			{
				var pointA = transform.position;
				var pointB = pointA + Random.onUnitSphere * radius;

				StartCoroutine(DelayedOnHit(pointA, pointB));
			}
		}
#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.DrawWireSphere(transform.position, radius);
		}
#endif
		private IEnumerator DelayedOnHit(Vector3 pointA, Vector3 pointB)
		{
			var vector      = pointB - pointA;
			var maxDistance = vector.magnitude;
			var ray         = new Ray(pointA, vector);
			var hit         = default(RaycastHit);

			if (Physics.Raycast(ray, out hit, maxDistance, layers) == true)
			{
				var distance01 = Mathf.InverseLerp(0.0f, radius, hit.distance);

				// Wait based on hit distance
				yield return new WaitForSeconds(distance01 * delayMax);

				var finalUp       = orientation == OrientationType.CameraUp ? P3dHelper.GetCameraUp(_camera) : Vector3.up;
				var finalPosition = hit.point + hit.normal * offset;
				var finalNormal   = normal == NormalType.HitNormal ? hit.normal : -ray.direction;
				var finalRotation = Quaternion.LookRotation(-finalNormal, finalUp);
				var finalPressure = 1.0f - distance01;

				hitCache.InvokePoint(gameObject, preview, priority, finalPressure, finalPosition, finalRotation);

				hitCache.InvokeRaycast(gameObject, preview, priority, finalPressure, hit, finalRotation);
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dHitExplosion))]
	public class P3dHitExplosion_Editor : P3dEditor<P3dHitExplosion>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Radius <= 0.0f));
				Draw("radius", "The radius of the explosion in world space.");
			EndError();
			BeginError(Any(t => t.Count <= 0));
				Draw("count", "The amount of rays that are cast.");
			EndError();
			BeginError(Any(t => t.DelayMax <= 0.0f));
				Draw("delayMax", "The maximum delay between explosion and impact in seconds.");
			EndError();
			BeginError(Any(t => t.Layers == 0));
				Draw("layers", "The layers you want the raycast to hit.");
			EndError();
			Draw("orientation", "How should the hit point be oriented?\nNone = It will be treated as a point with no rotation.\nWorldUp = It will be rotated to the normal, where the up vector is world up.\nCameraUp = It will be rotated to the normal, where the up vector is world up.");
			BeginIndent();
				if (Any(t => t.Orientation == P3dHitExplosion.OrientationType.CameraUp))
				{
					Draw("_camera", "Orient to a specific camera?\nNone = MainCamera.");
				}
			EndIndent();
			Draw("normal", "Which normal should the hit point rotation be based on?");
			Draw("offset", "If you want the raycast hit point to be offset from the surface a bit, this allows you to set by how much in world space.");

			Separator();

			Draw("preview", "Should the applied paint be applied as a preview?");
			Draw("priority", "This allows you to override the order this paint gets applied to the object during the current frame.");

			Separator();

			Target.HitCache.Inspector(Target.gameObject, true, false, false, false, true);
		}
	}
}
#endif