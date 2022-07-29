using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component auatomatically destroys the specified GameObject when you paint.</summary>
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dDestroyer")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Destroyer")]
	public class P3dDestroyer : MonoBehaviour, IHit, IHitPoint
	{
		/// <summary>This GameObject will be destroyed.</summary>
		public GameObject Target { set { target = value; } get { return target; } } [SerializeField] private GameObject target;

		[ContextMenu("Destroy Now")]
		public void DestroyNow()
		{
			Destroy(gameObject);
		}

		public void HandleHitPoint(bool preview, int priority, float pressure, int seed, Vector3 position, Quaternion rotation)
		{
			DestroyNow();
		}
#if UNITY_EDITOR
		protected virtual void Reset()
		{
			target = gameObject;
		}
#endif
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dDestroyer))]
	public class P3dDestroyer_Editor : P3dEditor<P3dDestroyer>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Target == null));
				Draw("target", "This GameObject will be destroyed.");
			EndError();
		}
	}
}
#endif