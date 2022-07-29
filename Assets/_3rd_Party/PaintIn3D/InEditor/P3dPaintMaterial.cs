using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	public class P3dPaintMaterial : ScriptableObject, P3dShaderTemplate.IHasTemplate
	{
		[System.Serializable]
		public class Slot
		{
			public P3dGroup Group;
			public Texture  Texture;
			public Color    Color = Color.white;
		}

		public enum StyleType
		{
			Seamless,
			Decal
		}

		public string Category { set { category = value; } get { return category; } } [SerializeField] private string category;

		public Texture2D Thumbnail { set { thumbnail = value; } get { return thumbnail; } } [SerializeField] private Texture2D thumbnail;

		public StyleType Style { set { style = value; } get { return style; } } [SerializeField] private StyleType style;

		public Texture Shape { set { shape = value; } get { return shape; } } [SerializeField] private Texture shape;

		public Material Material { set { material = value; } get { return material; } } [SerializeField] private Material material;

		public P3dShaderTemplate Template { set { template = value; } get { return template; } } [SerializeField] private P3dShaderTemplate template;

		public List<Slot> Slots { get { if (slots == null) slots = new List<Slot>(); return slots; } } [SerializeField] private List<Slot> slots;

		private static List<P3dPaintMaterial> cachedInstances = new List<P3dPaintMaterial>();

		private static bool cachedInstancesSet;

		public static void Draw(P3dPaintMaterial paintMaterial, Rect rect)
		{
			if (paintMaterial != null && paintMaterial.thumbnail != null)
			{
				GUI.DrawTexture(rect, paintMaterial.thumbnail);
			}
			else
			{
				GUI.DrawTexture(rect, Texture2D.whiteTexture);
			}
		}

		public void SetTemplate(P3dShaderTemplate template)
		{
			this.template = template;
		}

		public P3dShaderTemplate GetTemplate()
		{
			return template;
		}

		[ContextMenu("Clear Thumbnail")]
		public void ClearThumbnail()
		{
			DestroyImmediate(thumbnail);

			thumbnail = null;
		}

		public static void UpdateCachedInstances()
		{
			cachedInstancesSet = true;

			cachedInstances.Clear();

#if UNITY_EDITOR
			foreach (var guid in AssetDatabase.FindAssets("t:P3dPaintMaterial"))
			{
				var paint = AssetDatabase.LoadAssetAtPath<P3dPaintMaterial>(AssetDatabase.GUIDToAssetPath(guid));

				if (paint != null)
				{
					cachedInstances.Add(paint);
				}
			}
#endif
		}

		/// <summary>This static property returns a list of all cached <b>P3dGroupData</b> instances.
		/// NOTE: This will be empty in-game.</summary>
		public static List<P3dPaintMaterial> CachedInstances
		{
			get
			{
				if (cachedInstancesSet == false)
				{
					UpdateCachedInstances();
				}

				return cachedInstances;
			}
		}

		public Slot GetSlot(P3dGroup group)
		{
			foreach (var slot in Slots)
			{
				if (slot.Group == group)
				{
					return slot;
				}
			}

			return null;
		}

#if UNITY_EDITOR
		[MenuItem("Assets/Create/Paint in 3D/Paint Material")]
		private static void CreateAsset()
		{
			var asset = CreateInstance<P3dPaintMaterial>();
			var guids = Selection.assetGUIDs;
			var path  = guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;

			if (string.IsNullOrEmpty(path) == true)
			{
				path = "Assets";
			}
			else if (AssetDatabase.IsValidFolder(path) == false)
			{
				path = System.IO.Path.GetDirectoryName(path);
			}

			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + typeof(P3dPaintMaterial).ToString() + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();

			Selection.activeObject = asset; EditorGUIUtility.PingObject(asset);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dPaintMaterial))]
	public class P3dPaintMaterial_Editor : P3dEditor<P3dPaintMaterial>
	{
		protected virtual void OnEnable()
		{
			P3dPaintMaterial.UpdateCachedInstances();
		}

		protected override void OnInspector()
		{
			Draw("category");

			EditorGUILayout.Separator();

			foreach (var t in Targets)
			{
				if (P3dPaintMaterial.CachedInstances.Contains(t) == false)
				{
					P3dPaintMaterial.CachedInstances.Add(t);
				}
			}

			var sObj   = serializedObject;
			var sSlots = sObj.FindProperty("slots");

			Draw("style");
			if (Any(t => t.Style == P3dPaintMaterial.StyleType.Decal))
			{
				Draw("shape");
			}
			BeginError(Any(t => t.Material == null));
				Draw("material");
			EndError();
			BeginError(Any(t => t.Template == null));
				P3dShaderTemplate_Editor.DrawDropdown("Template", Target.Material, Target, sObj.FindProperty("previewTemplate"));
			EndError();

			var removeIndex = -1;

			for (var i = 0; i < sSlots.arraySize; i++)
			{
				var sSlot    = sSlots.GetArrayElementAtIndex(i);
				var sGroup   = sSlot.FindPropertyRelative("Group");
				var sTexture = sSlot.FindPropertyRelative("Texture");
				var sColor   = sSlot.FindPropertyRelative("Color");

				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				
				EditorGUILayout.PropertyField(sGroup);
				EditorGUILayout.PropertyField(sTexture);
				EditorGUILayout.PropertyField(sColor);

				P3dHelper.BeginColor(Color.red);
					if (GUILayout.Button("Remove Slot") == true)
					{
						removeIndex = i;
					}
				P3dHelper.EndColor();

				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.Separator();

			if (GUILayout.Button("Add Slot") == true)
			{
				sSlots.arraySize += 1;

				var sSlot = sSlots.GetArrayElementAtIndex(sSlots.arraySize - 1);

				sSlot.FindPropertyRelative("Group").enumValueIndex = 0;

				sSlot.FindPropertyRelative("Texture").objectReferenceValue = null;

				sSlot.FindPropertyRelative("Color").colorValue = Color.white;
			}

			if (removeIndex >= 0)
			{
				sSlots.DeleteArrayElementAtIndex(removeIndex);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif