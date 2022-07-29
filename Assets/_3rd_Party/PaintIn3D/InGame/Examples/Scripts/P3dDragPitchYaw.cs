using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component adds basic Pitch/Yaw controls to the current GameObject using mouse or touch input. This requires the P3dInputManager to be in your scene.</summary>
	[ExecuteInEditMode]
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dDragPitchYaw")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Drag Pitch Yaw")]
	public class P3dDragPitchYaw : MonoBehaviour
	{
		/// <summary>If this is set, then rotation will only activate when this GameObject is active.</summary>
		public Transform Requires { set { requires = value; } get { return requires; } } [SerializeField] private Transform requires;

		/// <summary>If you want rotation to require a specific key on desktop platforms, then you can specify it here.</summary>
		public KeyCode Key { set { key = value; } get { return key; } } [SerializeField] private KeyCode key = KeyCode.Mouse0;

		/// <summary>The target pitch angle in degrees.</summary>
		public float Pitch { set { pitch = value; } get { return pitch; } } [SerializeField] private float pitch;

		/// <summary>The speed the pitch changed relative to the mouse/finger drag distance.</summary>
		public float PitchSensitivity { set { pitchSensitivity = value; } get { return pitchSensitivity; } } [SerializeField] private float pitchSensitivity = 0.1f;

		/// <summary>The minimum value of the pitch value.</summary>
		public float PitchMin { set { pitchMin = value; } get { return pitchMin; } } [SerializeField] private float pitchMin = -90.0f;

		/// <summary>The maximum value of the pitch value.</summary>
		public float PitchMax { set { pitchMax = value; } get { return pitchMax; } } [SerializeField] private float pitchMax = 90.0f;

		/// <summary>The target yaw angle in degrees.</summary>
		public float Yaw { set { yaw = value; } get { return yaw; } } [SerializeField] private float yaw;

		/// <summary>The speed the yaw changed relative to the mouse/finger drag distance.</summary>
		public float YawSensitivity { set { yawSensitivity = value; } get { return yawSensitivity; } } [SerializeField] private float yawSensitivity = 0.1f;

		/// <summary>How quickly the rotation transitions from the current to the target value (-1 = instant).</summary>
		public float Dampening { set { dampening = value; } get { return dampening; } } [SerializeField] private float dampening = 10.0f;

		[SerializeField]
		private float currentPitch;

		[SerializeField]
		private float currentYaw;

		[System.NonSerialized]
		private P3dInputManager inputManager = new P3dInputManager();

		protected virtual void Update()
		{
			inputManager.Update(key);

			// Calculate delta
			if (requires == null || requires.gameObject.activeInHierarchy == true || Input.GetMouseButton(1) == true)
			{
				if (Application.isPlaying == true)
				{
					var delta = inputManager.GetAverageDeltaScaled();

					pitch -= delta.y * pitchSensitivity;
					yaw   += delta.x *   yawSensitivity;
				}
			}

			pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

			// Smoothly dampen values
			var factor = P3dHelper.DampenFactor(dampening, Time.deltaTime);

			currentPitch = Mathf.Lerp(currentPitch, pitch, factor);
			currentYaw   = Mathf.Lerp(currentYaw  , yaw  , factor);

			// Apply new rotation
			transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dDragPitchYaw))]
	public class P3dDragPitchYaw_Editor : P3dEditor<P3dDragPitchYaw>
	{
		protected override void OnInspector()
		{
			Draw("requires", "If this is set, then rotation will only activate when this GameObject is active.");
			Draw("key", "If you want rotation to require a specific key on desktop platforms, then you can specify it here.");

			Separator();

			Draw("pitch", "The target pitch angle in degrees.");
			Draw("pitchSensitivity", "The speed the camera rotates relative to the mouse/finger drag distance.");
			Draw("pitchMin", "The minimum value of the pitch value.");
			Draw("pitchMax", "The maximum value of the pitch value.");

			Separator();

			Draw("yaw", "The target yaw angle in degrees.");
			Draw("yawSensitivity", "The speed the yaw changed relative to the mouse/finger drag distance.");

			Separator();

			Draw("dampening", "How quickly the rotation transitions from the current to the target value (-1 = instant).");
		}
	}
}
#endif