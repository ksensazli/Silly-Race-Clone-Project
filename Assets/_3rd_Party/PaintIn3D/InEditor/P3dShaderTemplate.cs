using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	public class P3dShaderTemplate : ScriptableObject
	{
		public interface IHasTemplate
		{
			void SetTemplate(P3dShaderTemplate template);
			P3dShaderTemplate GetTemplate();
		}

		public enum Channel
		{
			Red,
			Green,
			Blue,
			Alpha
		}

		[System.Serializable]
		public class Write
		{
			public P3dGroup SourceGroup;

			public Channel SourceChannel;

			public Write GetCopy()
			{
				var write = new Write();

				write.SourceGroup   = SourceGroup;
				write.SourceChannel = SourceChannel;

				return write;
			}
		}

		[System.Serializable]
		public class Slot
		{
			public string Name;
			public string Alias;

			public Write WriteR;
			public Write WriteG;
			public Write WriteB;
			public Write WriteA;

			public string GetAlias()
			{
				if (string.IsNullOrEmpty(Alias) == true)
				{
					return Name;
				}

				return Alias;
			}
		}

		public static void UpdateCachedInstances()
		{
			cachedInstancesSet = true;

			cachedInstances.Clear();

#if UNITY_EDITOR
			foreach (var guid in AssetDatabase.FindAssets("t:P3dShaderTemplate"))
			{
				var template = AssetDatabase.LoadAssetAtPath<P3dShaderTemplate>(AssetDatabase.GUIDToAssetPath(guid));

				if (template != null)
				{
					cachedInstances.Add(template);
				}
			}
#endif
		}

		private static List<P3dShaderTemplate> tempInstances = new List<P3dShaderTemplate>();

		public static List<P3dShaderTemplate> GetTemplates(Shader shader)
		{
			tempInstances.Clear();

			if (cachedInstancesSet == false)
			{
				UpdateCachedInstances();
			}

			if (shader != null)
			{
				foreach (var instance in cachedInstances)
				{
					if (instance.Shader == shader)
					{
						tempInstances.Add(instance);
					}
				}
			}

			return tempInstances;
		}

		public Shader Shader { set { shader = value; } get { return shader; } } [SerializeField] private Shader shader;

		public List<Slot> Slots { get { if (slots == null) slots = new List<Slot>(); return slots; } } [SerializeField] private List<Slot> slots;

		private static List<P3dShaderTemplate> cachedInstances = new List<P3dShaderTemplate>();

		private static bool cachedInstancesSet;

#if UNITY_EDITOR
		[MenuItem("Assets/Create/Paint in 3D/Template")]
		private static void CreateAsset()
		{
			var asset = CreateInstance<P3dShaderTemplate>();
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

			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + typeof(P3dShaderTemplate).ToString() + ".asset");

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
	[CustomEditor(typeof(P3dShaderTemplate))]
	public class P3dShaderTemplate_Editor : P3dEditor<P3dShaderTemplate>
	{
		protected virtual void OnEnable()
		{
			P3dShaderTemplate.UpdateCachedInstances();
		}

		public static void DrawDropdown(string title, Material material, P3dShaderTemplate.IHasTemplate hasTemplate, SerializedProperty property = null)
		{
			var template = hasTemplate.GetTemplate();
			var rect     = P3dHelper.Reserve();
			var rectA    = rect; rectA.width = EditorGUIUtility.labelWidth;
			var rectB    = rect; rectB.xMin += EditorGUIUtility.labelWidth;

			EditorGUI.LabelField(rectA, title);

			if (material != null)
			{
				if (GUI.Button(rectB, template != null ? template.name : "", EditorStyles.popup) == true)
				{
					var menu           = new GenericMenu();
					var otherTemplates = P3dShaderTemplate.GetTemplates(material.shader);

					if (otherTemplates.Count == 0)
					{
						menu.AddDisabledItem(new GUIContent("No templates found for this material shader."));
					}

					for (var i = 0; i < otherTemplates.Count; i++)
					{
						var otherTemplate = otherTemplates[i];

						menu.AddItem(new GUIContent(otherTemplate.name), template == otherTemplate, () =>
							{
								if (property != null)
								{
									property.objectReferenceValue = otherTemplate;

									property.serializedObject.ApplyModifiedProperties();
								}
								else
								{
									hasTemplate.SetTemplate(otherTemplate);
								}
							});
					}

					menu.DropDown(rectB);
				}
			}
			else
			{
				EditorGUI.BeginDisabledGroup(true);
					GUI.Button(rectB, "Material Missing", EditorStyles.popup);
				EditorGUI.EndDisabledGroup();
			}
		}

		public static void DrawElement(SerializedProperty property)
		{
			var rect  = P3dHelper.Reserve();
			var rectA = rect; rectA.width = EditorGUIUtility.labelWidth;
			var rectB = rect; rectB.xMin += EditorGUIUtility.labelWidth; rectB.xMax -= 52.0f;
			var rectC = rect; rectC.xMin += EditorGUIUtility.labelWidth; rectC.xMin = rectC.xMax - 50.0f;

			EditorGUI.LabelField(rectA, new GUIContent(property.name, property.tooltip));

			P3dGroup_Drawer.Draw(rectB, property.FindPropertyRelative("SourceGroup"));

			EditorGUI.PropertyField(rectC, property.FindPropertyRelative("SourceChannel"), GUIContent.none);
		}

		public static void Draw(SerializedProperty sSlots, Shader shader)
		{
			var removeIndex = -1;

			for (var i = 0; i < sSlots.arraySize; i++)
			{
				var sSlot  = sSlots.GetArrayElementAtIndex(i);
				var sName  = sSlot.FindPropertyRelative("Name");
				var sAlias = sSlot.FindPropertyRelative("Alias");

				EditorGUILayout.BeginVertical(EditorStyles.helpBox);

				P3dHelper.BeginColor(P3dHelper.TexEnvNameExists(shader, sName.stringValue) == false);
				{
					var rect  = P3dHelper.Reserve();
					var rectA = rect; rectA.width = EditorGUIUtility.labelWidth;
					var rectB = rect; rectB.xMin += EditorGUIUtility.labelWidth; rectB.xMax -= 20;
					var rectC = rect; rectC.xMin = rectC.xMax - 18;

					EditorGUI.LabelField(rectA, new GUIContent(sName.name, sName.tooltip));
					EditorGUI.PropertyField(rectB, sName, GUIContent.none);

					// Draw menu
					if (GUI.Button(rectC, "", EditorStyles.popup) == true)
					{
						var menu    = new GenericMenu();
						var texEnvs = P3dHelper.GetTexEnvs(shader);

						if (texEnvs != null && texEnvs.Count > 0)
						{
							for (var j = 0; j < texEnvs.Count; j++)
							{
								var texName = texEnvs[j].Name;

								menu.AddItem(new GUIContent(texName), sName.stringValue == texName, () => { sName.stringValue = texName; sName.serializedObject.ApplyModifiedProperties(); });
							}
						}
						else
						{
							menu.AddDisabledItem(new GUIContent("This shader has no textures!"));
						}

						menu.DropDown(rectC);
					}
				}
				P3dHelper.EndColor();

				EditorGUILayout.PropertyField(sAlias);

				DrawElement(sSlot.FindPropertyRelative("WriteR"));
				DrawElement(sSlot.FindPropertyRelative("WriteG"));
				DrawElement(sSlot.FindPropertyRelative("WriteB"));
				DrawElement(sSlot.FindPropertyRelative("WriteA"));

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
			}

			if (removeIndex >= 0)
			{
				sSlots.DeleteArrayElementAtIndex(removeIndex);
			}
		}

		protected override void OnInspector()
		{
			Draw("shader", "");

			var sObj   = serializedObject;
			var sSlots = sObj.FindProperty("slots");

			Draw(sSlots, Target.Shader);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif