#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		private void UpdateMaterialsPanel()
		{
			for (var i = 0; i < scene.Mats.Count; i++)
			{
				EditorGUILayout.Space();

				var mat     = scene.Mats[i];
				var matRect = 
				EditorGUILayout.BeginVertical(GetSelectableStyle(mat == currentMat, true));
					P3dHelper.BeginLabelWidth(60.0f);
						if (mat == currentMat)
						{
							EditorGUILayout.BeginHorizontal();
								P3dHelper.BeginColor(scene.MatNameValid(mat.Name) == false);
									mat.Name = EditorGUILayout.TextField(mat.Name);
								P3dHelper.EndColor();
								if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)) == true && EditorUtility.DisplayDialog("Are you sure?", "This will delete the current material from the paint window.", "Delete") == true)
								{
									scene.Mats.RemoveAt(i--); P3dHelper.ClearControl();
								}
							EditorGUILayout.EndHorizontal();
							P3dHelper.BeginColor(mat.Material == null);
								mat.Material = (Material)EditorGUILayout.ObjectField("Material", mat.Material, typeof(Material), true);
							P3dHelper.EndColor();
							P3dHelper.BeginColor(mat.Template == null);
								P3dShaderTemplate_Editor.DrawDropdown("Template", mat.Material, mat);
							P3dHelper.EndColor();
							mat.Width  = EditorGUILayout.IntField("Width", mat.Width);
							mat.Height = EditorGUILayout.IntField("Height", mat.Height);

							//foreach (var slot in mat.Slots)
							//{
							//	EditorGUILayout.ObjectField(slot.Texture, typeof(Texture), false);
							//}

							//foreach (var layer in mat.MergedLayers.Values)
							//{
							//	EditorGUILayout.ObjectField(layer.Under, typeof(Texture), false);
							//	EditorGUILayout.ObjectField(layer.Above, typeof(Texture), false);
							//	EditorGUILayout.ObjectField(layer.Final, typeof(Texture), false);
							//}

							if (mat.SizesMatch == false && GUILayout.Button("Resize") == true)
							{
								mat.Resize();
							}
						}
						else
						{
							EditorGUILayout.LabelField(mat.Name);
						}
					P3dHelper.EndLabelWidth();
				EditorGUILayout.EndVertical();

				if (Event.current.type == EventType.MouseDown && matRect.Contains(Event.current.mousePosition) == true)
				{
					currentMat = mat; P3dHelper.ClearControl();
				}
			}

			if (GUILayout.Button("Add Material") == true)
			{
				currentMat = scene.AddMat(null, null, settings.DefaultTextureSize); P3dHelper.ClearControl();
			}
		}
	}
}
#endif