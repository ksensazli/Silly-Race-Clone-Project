using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This object allows you to switch P3D material shaders between standard shader usage, and scriptable render pipelines (SRP) shaders.</summary>
	[ExecuteInEditMode]
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dRenderingPipeline")]
	public class P3dRenderingPipeline : ScriptableObject
	{
		public static bool IsScriptable
		{
			get
			{
				if (instance == null)
				{
					instance = Resources.Load<P3dRenderingPipeline>("P3dRenderingPipeline");
				}

				if (instance != null)
				{
					return instance.isScriptable;
				}

				return false;
			}
		}

		private static P3dRenderingPipeline instance;

		[SerializeField]
		#pragma warning disable 649
		private bool isScriptable;
		#pragma warning restore 649

		#pragma warning disable 67
		public static event System.Action<bool> OnPipelineChanged;
		#pragma warning restore 67

#if UNITY_EDITOR
		[ContextMenu("Apply Standard")]
		public void ApplyStandard()
		{
			Apply(false);
		}

		[ContextMenu("Apply Scriptable")]
		public void ApplyScriptable()
		{
			Apply(true);
		}

		private void Apply(bool srp)
		{
			var map      = new Dictionary<Shader, Shader>();
			var text     = default(string);
			var shaders  = AssetDatabase.FindAssets("t:Shader").Select(g => AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(g))).Where(s => s.name.Contains("P3D"));
			var shadersA = shaders.Where(s => s.name.EndsWith("SRP") == false);
			var shadersB = shaders.Where(s => s.name.EndsWith("SRP") == true );

			foreach (var shaderA in shadersA)
			{
				var shaderBName = shaderA.name.Substring(shaderA.name.LastIndexOf("/")) + " SRP";
				var shaderB     = shadersB.FirstOrDefault(s => s.name.EndsWith(shaderBName));

				if (shaderB != null)
				{
					if (srp == true)
					{
						map.Add(shaderA, shaderB);
					}
					else
					{
						map.Add(shaderB, shaderA);
					}
				}
			}

			text = "REPLACING";

			foreach (var pair in map)
			{
				text += "\n'" + pair.Key.name + "' with '" + pair.Value.name + "'";
			}

			Debug.Log(text);

			foreach (var material in AssetDatabase.FindAssets("t:Material").Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))))
			{
				if (material != null)
				{
					var replacement = default(Shader);

					if (map.TryGetValue(material.shader, out replacement) == true)
					{
						material.shader = replacement;
					}
				}
			}

			if (srp == true)
			{
				text = "REIMPORTING";

				foreach (var shaderB in shadersB)
				{
					var path = AssetDatabase.GetAssetPath(shaderB);

					text += "\n" + path;

					AssetDatabase.ImportAsset(path);
				}
			}

			isScriptable = srp;

			if (OnPipelineChanged != null)
			{
				OnPipelineChanged(isScriptable);
			}

			Debug.Log(srp ? "Finished Switching to Scriptable Pipeline" : "Finished Switching to Standard Pipeline");
		}
#endif
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	[CustomEditor(typeof(P3dRenderingPipeline))]
	public class P3dRenderingPipeline_Editor : Editor
	{
		public override void OnInspectorGUI()
		{
			var Target = (P3dRenderingPipeline)target;

			EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("isScriptable"));
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Separator();

			if (GUILayout.Button("Switch To Standard Pipeline") == true)
			{
				Target.ApplyStandard();
				
				EditorUtility.SetDirty(Target);

				serializedObject.Update();
			}

			if (GUILayout.Button("Switch To Scriptable Pipeline") == true)
			{
				Target.ApplyScriptable();
				
				EditorUtility.SetDirty(Target);

				serializedObject.Update();
			}
		}
	}
}
#endif