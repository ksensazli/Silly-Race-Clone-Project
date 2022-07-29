using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This defines the blending mode used by a painting operation.</summary>
	[System.Serializable]
	public struct P3dBlendMode
	{
		public static P3dBlendMode AlphaBlend { get { return new P3dBlendMode() { Index = ALPHA_BLEND, Color = Color.white }; } }
		public static P3dBlendMode AlphaBlendInverse { get { return new P3dBlendMode() { Index = ALPHA_BLEND_INVERSE, Color = Color.white }; } }
		public static P3dBlendMode AlphaBlendRGB { get { return new P3dBlendMode() { Index = ALPHA_BLEND_RGB, Color = Color.white }; } }
		public static P3dBlendMode Additive { get { return new P3dBlendMode() { Index = ADDITIVE, Color = Color.white }; } }
		public static P3dBlendMode AdditiveSoft { get { return new P3dBlendMode() { Index = ADDITIVE_SOFT, Color = Color.white }; } }
		public static P3dBlendMode Subtractive { get { return new P3dBlendMode() { Index = SUBTRACTIVE, Color = Color.white }; } }
		public static P3dBlendMode SubtractiveSoft { get { return new P3dBlendMode() { Index = SUBTRACTIVE_SOFT, Color = Color.white }; } }
		public static P3dBlendMode Replace { get { return new P3dBlendMode() { Index = REPLACE, Color = Color.white }; } }
		public static P3dBlendMode ReplaceOriginal { get { return new P3dBlendMode() { Index = REPLACE_ORIGINAL, Color = Color.white }; } }
		public static P3dBlendMode ReplaceCustom(Color color, Texture texture) { return new P3dBlendMode() { Index = REPLACE_CUSTOM, Color = color, Texture = texture }; }
		public static P3dBlendMode MultiplyInverseRGB { get { return new P3dBlendMode() { Index = MULTIPLY_INVERSE_RGB, Color = Color.white }; } }
		public static P3dBlendMode Blur { get { return new P3dBlendMode() { Index = BLUR, Color = Color.white }; } }

		// Indices of all blending modes
		public const int ALPHA_BLEND          = 0;
		public const int ALPHA_BLEND_INVERSE  = 1;
		public const int ALPHA_BLEND_RGB      = 2;
		public const int ADDITIVE             = 3;
		public const int ADDITIVE_SOFT        = 4;
		public const int SUBTRACTIVE          = 5;
		public const int SUBTRACTIVE_SOFT     = 6;
		public const int REPLACE              = 7;
		public const int REPLACE_ORIGINAL     = 8;
		public const int REPLACE_CUSTOM       = 9;
		public const int MULTIPLY_INVERSE_RGB = 10;
		public const int BLUR                 = 11;
		public const int COUNT                = 12;

		// Pretty names of all blending modes
		public static readonly string[] NAMES =
			{
				"Alpha Blend",
				"Alpha Blend Inverse",
				"Alpha Blend RGB",
				"Additive",
				"Additive Soft",
				"Subtractive",
				"Subtractive Soft",
				"Replace",
				"Replace Original",
				"Replace Custom",
				"Multiply RGB Inverse",
				"Blur"
			};

		/// <summary>This is the index of the currently selected blending mode.</summary>
		public int Index;

		/// <summary>When using the <b>ReplaceTexture</b> blending mode, this allows you to specify the replacement texture.</summary>
		public Texture Texture;

		/// <summary>When using the <b>ReplaceTexture</b> blending mode, this allows you to specify the replacement color.</summary>
		public Color Color;

		public void Apply(Material material)
		{
			if (Index == REPLACE_CUSTOM || Index == REPLACE_ORIGINAL)
			{
				material.SetColor(P3dShader._ReplaceColor, Color);
				material.SetTexture(P3dShader._ReplaceTexture, Texture);
			}
		}

		public static string GetName(int index)
		{
			if (index >= 0 && index < COUNT)
			{
				return NAMES[index];
			}

			return default(string);
		}

		public static bool operator == (P3dBlendMode a, int b)
		{
			return a.Index == b;
		}

		public static bool operator != (P3dBlendMode a, int b)
		{
			return a.Index != b;
		}

		public static implicit operator int (P3dBlendMode a)
		{
			return a.Index;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	[CustomPropertyDrawer(typeof(P3dBlendMode))]
	public class P3dBlendMode_Drawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var index  = property.FindPropertyRelative("Index").intValue;
			var height = base.GetPropertyHeight(property, label);
			var step   = height + 2;

			if (index == P3dBlendMode.REPLACE_CUSTOM)
			{
				height += step * 2;
			}

			return height;
		}

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
		{
			var sObj   = property.serializedObject;
			var sIdx   = property.FindPropertyRelative("Index");
			var sCol   = property.FindPropertyRelative("Color");
			var sTex   = property.FindPropertyRelative("Texture");
			var height = base.GetPropertyHeight(property, label);

			rect.height = height;

			var right = rect; right.xMin += EditorGUIUtility.labelWidth;

			EditorGUI.LabelField(rect, label);

			if (GUI.Button(right, P3dBlendMode.GetName(sIdx.intValue), EditorStyles.popup) == true)
			{
				var menu = new GenericMenu();

				for (var i = 0; i < P3dBlendMode.COUNT; i++)
				{
					var index   = i;
					var content = new GUIContent(P3dBlendMode.GetName(index));
					var on      = index == sIdx.intValue;

					menu.AddItem(content, on, () => { sIdx.intValue = index; sObj.ApplyModifiedProperties(); });
				}

				menu.DropDown(right);
			}

			if (sIdx.intValue == P3dBlendMode.REPLACE_CUSTOM)
			{
				EditorGUI.indentLevel++;
					rect.y += rect.height + 2; EditorGUI.PropertyField(rect, sCol);
					rect.y += rect.height + 2; EditorGUI.PropertyField(rect, sTex);
				EditorGUI.indentLevel--;
			}
		}

		private void DrawObjectProperty<T>(ref Rect rect, SerializedProperty property, string title)
			where T : Object
		{
			var propertyObject = property.FindPropertyRelative("Object");
			var oldValue       = propertyObject.objectReferenceValue as T;
			var mixed          = EditorGUI.showMixedValue; EditorGUI.showMixedValue = propertyObject.hasMultipleDifferentValues;
				var newValue = EditorGUI.ObjectField(rect, title, oldValue, typeof(T), true);
			EditorGUI.showMixedValue = mixed;

			if (oldValue != newValue)
			{
				propertyObject.objectReferenceValue = newValue;
			}
		}

		private void DrawProperty(ref Rect rect, SerializedProperty property, GUIContent label, string childName, string overrideName = null, string overrideTooltip = null)
		{
			var childProperty = property.FindPropertyRelative(childName);

			label.text = string.IsNullOrEmpty(overrideName) == false ? overrideName : childProperty.displayName;

			label.tooltip = string.IsNullOrEmpty(overrideTooltip) == false ? overrideTooltip : childProperty.tooltip;

			EditorGUI.PropertyField(rect, childProperty, label);
		}
	}
}
#endif