using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This object allows you to define information about a paint group like its name, which can then be selected using the <b>P3dGroup</b> setting on components like <b>P3dPaintableTexture</b> and <b>P3dPaintDecal</b>.</summary>
	public class P3dGroupData : ScriptableObject
	{
		/// <summary>This allows you to set the ID of this group (e.g. 100).
		/// NOTE: This number should be unique, and not shared by any other <b>P3dGroupData</b>.</summary>
		public int Index { set { index = value; } get { return index; } } [SerializeField] private int index;

		/// <summary>This allows you to set the default color/tint of textures created in this group.
		/// NOTE: This is used for in-editor painting only.</summary>
		public Color DefaultColor { set { defaultColor = value; } get { return defaultColor; } } [SerializeField] private Color defaultColor;

		/// <summary>This allows you to set the default texture color of textures created in this group.
		/// NOTE: This is used for in-editor painting only.</summary>
		public Texture DefaultTexture { set { defaultTexture = value; } get { return defaultTexture; } } [SerializeField] private Texture defaultTexture;

		private static List<P3dGroupData> cachedInstances = new List<P3dGroupData>();

		private static bool cachedInstancesSet;

		/// <summary>This method allows you to get the <b>name</b> of the current group, with an optional prefix of the <b>Index</b> (e.g. "100: Albedo").</summary>
		public string GetName(bool prefixNumber)
		{
			if (prefixNumber == true)
			{
				return index + ": " + name;
			}

			return name;
		}

		/// <summary>This static method forces the cached instance list to update.
		/// NOTE: This does nothing in-game.</summary>
		public static void UpdateCachedInstances()
		{
			cachedInstancesSet = true;
#if UNITY_EDITOR
			cachedInstances.Clear();

			foreach (var guid in AssetDatabase.FindAssets("t:P3dGroupData"))
			{
				var groupName = AssetDatabase.LoadAssetAtPath<P3dGroupData>(AssetDatabase.GUIDToAssetPath(guid));

				cachedInstances.Add(groupName);
			}
#endif
		}

		/// <summary>This static property returns a list of all cached <b>P3dGroupData</b> instances.
		/// NOTE: This will be empty in-game.</summary>
		public static List<P3dGroupData> CachedInstances
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

		/// <summary>This static method calls <b>GetAlias</b> on the <b>P3dGroupData</b> with the specified <b>Index</b> setting, or null.</summary>
		public static string GetGroupName(int index, bool prefixNumber)
		{
			var groupData = GetGroupData(index);

			return groupData != null ? groupData.GetName(prefixNumber) : null;
		}

		/// <summary>This static method returns the <b>P3dGroupData</b> with the specified <b>Index</b> setting, or null.</summary>
		public static P3dGroupData GetGroupData(int index)
		{
			foreach (var cachedGroupName in CachedInstances)
			{
				if (cachedGroupName != null && cachedGroupName.index == index)
				{
					return cachedGroupName;
				}
			}

			return null;
		}

#if UNITY_EDITOR
		[MenuItem("Assets/Create/Paint in 3D/Group Data")]
		private static void CreateAsset()
		{
			var asset = CreateInstance<P3dGroupData>();
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

			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + typeof(P3dGroupData).ToString() + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();

			Selection.activeObject = asset; EditorGUIUtility.PingObject(asset);

			cachedInstances.Add(asset);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dGroupData))]
	public class P3dGroupName_Editor : P3dEditor<P3dGroupData>
	{
		protected virtual void OnEnable()
		{
			P3dGroupData.UpdateCachedInstances();
		}

		protected override void OnInspector()
		{
			var clashes = P3dGroupData.CachedInstances.Where(d => d.Index == Target.Index);

			BeginError(clashes.Count() > 1);
				Draw("index", "This allows you to set the ID of this group (e.g. 100).\n\nNOTE: This number should be unique, and not shared by any other <b>P3dGroupData</b>.");
			EndError();

			foreach (var clash in clashes)
			{
				if (clash != Target)
				{
					EditorGUILayout.HelpBox("This index is also used by the " + clash.name + " group!", MessageType.Error);
				}
			}

			Separator();

			Draw("defaultColor", "This allows you to set the default color/tint of textures created in this group.\n\nNOTE: This is used for in-editor painting only.");
			Draw("defaultTexture", "This allows you to set the default texture color of textures created in this group.\n\nNOTE: This is used for in-editor painting only.");

			Separator();

			EditorGUILayout.LabelField("Current Groups", EditorStyles.boldLabel);

			var groupDatas  = P3dGroupData.CachedInstances.OrderBy(d => d.Index);
			var editorCount = 0;

			EditorGUI.BeginDisabledGroup(true);
				foreach (var groupData in groupDatas)
				{
					if (groupData != null)
					{
						if (groupData.Index >= 0)
						{
							EditorGUILayout.BeginHorizontal();
								EditorGUILayout.LabelField(groupData.name);
								EditorGUILayout.IntField(groupData.Index);
							EditorGUILayout.EndHorizontal();
						}
						else
						{
							editorCount++;
						}
					}
				}

				if (editorCount > 0)
				{
					EditorGUILayout.Separator();

					GUILayout.Label("EDITOR");

					EditorGUILayout.Separator();

					foreach (var groupData in groupDatas)
					{
						if (groupData != null && groupData.Index < 0)
						{
							EditorGUILayout.BeginHorizontal();
								EditorGUILayout.LabelField(groupData.name);
								EditorGUILayout.IntField(groupData.Index);
							EditorGUILayout.EndHorizontal();
						}
					}
				}
			EditorGUI.EndDisabledGroup();
		}
	}
}
#endif