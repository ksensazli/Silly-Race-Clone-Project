using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This class allows you to change the painting opacity based on the paint pressure.</summary>
	[System.Serializable]
	public class P3dModifyOpacityPressure : P3dModifier
	{
		public enum BlendType
		{
			Replace,
			Multiply,
			Increment
		}

		public static string Group = "Opacity";

		public static string Title = "Pressure";

		/// <summary>The change in opacity when the pressure is 1, based on the current blend.</summary>
		public float Opacity { set { opacity = value; } get { return opacity; } } [SerializeField] private float opacity = 1.0f;

		/// <summary>The way the opacity value will be blended with the current one.</summary>
		public BlendType Blend { set { blend = value; } get { return blend; } } [SerializeField] private BlendType blend;

		protected override void OnModifyOpacity(ref float currentOpacity, float pressure)
		{
			var targetOpacity = default(float);

			switch (blend)
			{
				case BlendType.Replace:
				{
					targetOpacity = opacity;
				}
				break;

				case BlendType.Multiply:
				{
					targetOpacity = currentOpacity * opacity;
				}
				break;

				case BlendType.Increment:
				{
					targetOpacity = currentOpacity + opacity;
				}
				break;
			}

			currentOpacity += (targetOpacity - currentOpacity) * pressure;
		}
#if UNITY_EDITOR
		public override void DrawEditorLayout()
		{
			opacity = EditorGUILayout.FloatField(new GUIContent("Opacity", "The change in opacity when the pressure is 1, based on the current blend."), opacity);
			blend = (BlendType)EditorGUILayout.EnumPopup(new GUIContent("Blend", "The way the opacity value will be blended with the current one."), blend);
		}
#endif
	}
}