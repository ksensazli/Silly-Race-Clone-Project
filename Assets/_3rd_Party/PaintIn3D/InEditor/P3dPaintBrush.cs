using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	public class P3dPaintBrush : ScriptableObject
	{
		[System.Serializable]
		public class SharedData
		{
			public Texture Shape { set { shape = value; } get { return shape; } } [SerializeField] private Texture shape;

			public P3dChannel ShapeChannel { set { shapeChannel = value; } get { return shapeChannel; } } [SerializeField] private P3dChannel shapeChannel;

			public P3dModifierList Modifiers { get { if (modifiers == null) modifiers = new P3dModifierList(); return modifiers; } } [SerializeField] private P3dModifierList modifiers;

			public void Draw(Rect rect)
			{
				if (shape != null)
				{
					GUI.DrawTexture(rect, shape);
				}
				else
				{
					GUI.DrawTexture(rect, Texture2D.whiteTexture);
				}
			}
		}

		public string Category { set { category = value; } get { return category; } } [SerializeField] private string category;

		public SharedData Data { get { if (data == null) data = new SharedData(); return data; } } [SerializeField] private SharedData data;

		private static List<P3dPaintBrush> cachedInstances = new List<P3dPaintBrush>();

		private static bool cachedInstancesSet;

		public static void UpdateCachedInstances()
		{
			cachedInstancesSet = true;

			cachedInstances.Clear();

#if UNITY_EDITOR
			foreach (var guid in AssetDatabase.FindAssets("t:P3dPaintBrush"))
			{
				var brush = AssetDatabase.LoadAssetAtPath<P3dPaintBrush>(AssetDatabase.GUIDToAssetPath(guid));

				if (brush != null)
				{
					cachedInstances.Add(brush);
				}
			}
#endif
		}

		/// <summary>This static property returns a list of all cached <b>P3dGroupData</b> instances.
		/// NOTE: This will be empty in-game.</summary>
		public static List<P3dPaintBrush> CachedInstances
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

#if UNITY_EDITOR
		[MenuItem("Assets/Create/Paint in 3D/Paint Brush")]
		private static void CreateAsset()
		{
			var asset = CreateInstance<P3dPaintBrush>();
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

			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + typeof(P3dPaintBrush).ToString() + ".asset");

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
	[CustomEditor(typeof(P3dPaintBrush))]
	public class P3dPaintBrush_Editor : P3dEditor<P3dPaintBrush>
	{
		protected override void OnInspector()
		{
			Draw("category");

			EditorGUILayout.Separator();

			foreach (var t in Targets)
			{
				if (P3dPaintBrush.CachedInstances.Contains(t) == false)
				{
					P3dPaintBrush.CachedInstances.Add(t);
				}
			}
			EditorGUILayout.BeginHorizontal();
				BeginError(Any(t => t.Data.Shape == null));
					Draw("data.shape");
				EndError();
				EditorGUILayout.PropertyField(serializedObject.FindProperty("data.shapeChannel"), GUIContent.none, GUILayout.Width(50));
			EditorGUILayout.EndHorizontal();

			GUILayout.FlexibleSpace();

			Target.Data.Modifiers.DrawEditorLayout(false, "Angle", "Opacity", "Position", "Radius");
		}
	}
}
#endif