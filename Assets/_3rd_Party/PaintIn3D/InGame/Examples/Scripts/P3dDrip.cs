using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component makes the current gameObject follow the specified camera..</summary>
	[ExecuteInEditMode]
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dDrip")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Drip")]
	public class P3dDrip : MonoBehaviour
	{
		/// <summary>The speed this GameObject moves down in world units per second.</summary>
		public float Speed { set { speed = value; } get { return speed; } } [SerializeField] private float speed = 1.0f;

		/// <summary>The speed at which the Speed value reaches 0 (0 = no dampening).</summary>
		public float Dampening { set { dampening = value; } get { return dampening; } } [SerializeField] private float dampening = 1.0f;

		protected virtual void LateUpdate()
		{
			transform.position += Vector3.down * speed * Time.deltaTime;

			var factor = P3dHelper.DampenFactor(dampening, Time.deltaTime);

			speed = Mathf.Lerp(speed, 0.0f, factor);
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dDrip))]
	public class P3dDrip_Editor : P3dEditor<P3dDrip>
	{
		protected override void OnInspector()
		{
			Draw("speed", "The speed this GameObject moves down in world units per second.");
			Draw("dampening", "The speed at which the Speed value reaches 0 (0 = no dampening).");
		}
	}
}
#endif