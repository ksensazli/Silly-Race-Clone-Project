using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This class allows you to change the painting hardness based on the paint pressure.</summary>
	public class P3dModifyHardnessPressure : P3dModifier
	{
		public enum BlendType
		{
			Replace,
			Multiply,
			Increment
		}

		public static string Group = "Hardness";

		public static string Title = "Pressure";

		/// <summary>The change in hardness when the pressure is 1, based on the current blend.</summary>
		public float Hardness { set { hardness = value; } get { return hardness; } } [SerializeField] private float hardness = 1.0f;

		/// <summary>The way the hardness value will be blended with the current one.</summary>
		public BlendType Blend { set { blend = value; } get { return blend; } } [SerializeField] private BlendType blend;

		protected override void OnModifyHardness(ref float currentHardness, float pressure)
		{
			var targetHardness = default(float);

			switch (blend)
			{
				case BlendType.Replace:
				{
					targetHardness = hardness;
				}
				break;

				case BlendType.Multiply:
				{
					targetHardness = currentHardness * hardness;
				}
				break;

				case BlendType.Increment:
				{
					targetHardness = currentHardness + hardness;
				}
				break;
			}

			currentHardness += (targetHardness - currentHardness) * pressure;
		}
#if UNITY_EDITOR
		public override void DrawEditorLayout()
		{
			hardness = EditorGUILayout.FloatField(new GUIContent("Hardness", "The change in hardness when the pressure is 1, based on the current blend."), hardness);
			blend = (BlendType)EditorGUILayout.EnumPopup(new GUIContent("Blend", "The way the hardness value will be blended with the current one."), blend);
		}
#endif
	}
}