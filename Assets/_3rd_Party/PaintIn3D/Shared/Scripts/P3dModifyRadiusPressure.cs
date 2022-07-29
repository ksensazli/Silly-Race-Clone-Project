using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This class allows you to change the painting radius based on the paint pressure.</summary>
	[System.Serializable]
	public class P3dModifyRadiusPressure : P3dModifier
	{
		public enum BlendType
		{
			Replace,
			Multiply,
			Increment
		}

		public static string Group = "Radius";

		public static string Title = "Pressure";

		/// <summary>The change in radius when the pressure is 1, based on the current blend.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [SerializeField] private float radius = 1.0f;

		/// <summary>The way the radius value will be blended with the current one.</summary>
		public BlendType Blend { set { blend = value; } get { return blend; } } [SerializeField] private BlendType blend;

		protected override void OnModifyRadius(ref float currentRadius, float pressure)
		{
			var targetRadius = default(float);

			switch (blend)
			{
				case BlendType.Replace:
				{
					targetRadius = radius;
				}
				break;

				case BlendType.Multiply:
				{
					targetRadius = currentRadius * radius;
				}
				break;

				case BlendType.Increment:
				{
					targetRadius = currentRadius + radius;
				}
				break;
			}

			currentRadius += (targetRadius - currentRadius) * pressure;
		}
#if UNITY_EDITOR
		public override void DrawEditorLayout()
		{
			radius = EditorGUILayout.FloatField(new GUIContent("Radius", "The change in radius when the pressure is 1, based on the current blend."), radius);
			blend = (BlendType)EditorGUILayout.EnumPopup(new GUIContent("Blend", "The way the picked radius value will be blended with the current one."), blend);
		}
#endif
	}
}