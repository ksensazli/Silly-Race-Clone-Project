#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		class TempObj : P3dShaderTemplate.IHasTemplate
		{
			public bool        Dirty;
			public GameObject  Source;
			public int         Index;
			public P3dShaderTemplate Template;

			public void SetTemplate(P3dShaderTemplate template)
			{
				Template = template;
			}

			public P3dShaderTemplate GetTemplate()
			{
				return Template;
			}
		}

		private List<TempObj> tempObjs = new List<TempObj>();

		private List<P3dShaderTemplate> tempTemplates = new List<P3dShaderTemplate>();

		private TempObj GetTempObj(GameObject source, int index)
		{
			var tempObj = tempObjs.Find(t => t.Source == source && t.Index == index);

			if (tempObj != null)
			{
				tempObj.Dirty = false;
			}
			else
			{
				tempObj = new TempObj();

				tempObj.Source = source;
				tempObj.Index  = index;
			}

			return tempObj;
		}

		private void UpdateObjectsPanel()
		{
			if (Selection.gameObjects.Length == 0 && scene.Objs.Count == 0)
			{
				EditorGUILayout.HelpBox("Select a GameObject with a MesFilter+MeshRenderer or SkinnedMeshRenderer.", MessageType.Info);
			}

			// Mark
			tempObjs.ForEach(t => t.Dirty = true);

			foreach (var go in Selection.gameObjects)
			{
				if (scene.ObjExists(go.transform) == false)
				{
					var mf = go.GetComponent<MeshFilter>();
					var mr = go.GetComponent<MeshRenderer>();

					if (mf != null && mr != null && mf.sharedMesh != null)
					{
						GUILayout.BeginVertical(EditorStyles.helpBox);
							EditorGUI.BeginDisabledGroup(true);
								EditorGUILayout.ObjectField(go, typeof(GameObject), true);
							EditorGUI.EndDisabledGroup();

							var materials = mr.sharedMaterials;

							tempTemplates.Clear();

							for (var i = 0; i < materials.Length; i++)
							{
								var material  = materials[i];
								var templates = P3dShaderTemplate.GetTemplates(material != null ? material.shader : null);
								var slot      = GetTempObj(go, i);

								if (templates.Contains(slot.Template) == false)
								{
									slot.Template = templates.Count > 0 ? templates[0] : null;
								}

								GUILayout.BeginVertical(EditorStyles.helpBox);
									P3dHelper.BeginLabelWidth(60.0f);
										EditorGUI.BeginDisabledGroup(true);
												EditorGUILayout.ObjectField("Material", material, typeof(Material), true);
										EditorGUI.EndDisabledGroup();
										P3dHelper.BeginColor(slot.Template == null);
											P3dShaderTemplate_Editor.DrawDropdown("Template", material, slot);
										P3dHelper.EndColor();
									P3dHelper.EndLabelWidth();
								GUILayout.EndVertical();

								tempTemplates.Add(slot.Template);
							}

							if (GUILayout.Button("Add") == true)
							{
								scene.AddObj(go.transform, mf.sharedMesh, go.transform.position, go.transform.rotation, go.transform.lossyScale, materials, tempTemplates.ToArray(), settings.DefaultTextureSize);
							}
						GUILayout.EndVertical();
					}
				}
			}

			// Sweep
			tempObjs.RemoveAll(t => t.Dirty == true);

			EditorGUILayout.Separator();

			for (var i = 0; i < scene.Objs.Count; i++)
			{
				if (i > 0) EditorGUILayout.Space();

				var obj     = scene.Objs[i];
				var objRect =
				EditorGUILayout.BeginVertical(GetSelectableStyle(obj == currentObj, true));
					P3dHelper.BeginLabelWidth(60.0f);
						if (obj == currentObj)
						{
							EditorGUILayout.BeginHorizontal();
								obj.Name = EditorGUILayout.TextField(obj.Name);
								if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)) == true && EditorUtility.DisplayDialog("Are you sure?", "This will delete the current layer from the paint window.", "Delete") == true)
								{
									scene.RemoveObj(obj); i--; P3dHelper.ClearControl();
								}
							EditorGUILayout.EndHorizontal();

							obj.Mesh      = (Mesh)EditorGUILayout.ObjectField("Mesh", obj.Mesh, typeof(Mesh), true);
							obj.Paintable = EditorGUILayout.Toggle("Paintable", obj.Paintable);
							obj.Coord     = (P3dCoord)EditorGUILayout.EnumPopup("Coord", obj.Coord);
							obj.Transform = (Transform)EditorGUILayout.ObjectField("Transform", obj.Transform, typeof(Transform), true);

							if (obj.Transform == null)
							{
								obj.Position = EditorGUILayout.Vector3Field("Position", obj.Position);

								EditorGUI.BeginChangeCheck();
									var newRot = EditorGUILayout.Vector3Field("Rotation", obj.Rotation.eulerAngles);
								if (EditorGUI.EndChangeCheck() == true)
								{
									obj.Rotation = Quaternion.Euler(newRot);
								}

								obj.Scale = EditorGUILayout.Vector3Field("Scale", obj.Scale);
							}

							EditorGUILayout.BeginHorizontal();
								EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
								if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)) == true)
								{
									obj.MatIds.Add(-1);
								}
							EditorGUILayout.EndHorizontal();

							for (var j = 0; j < obj.MatIds.Count; j++)
							{
								var matId = obj.MatIds[j];
								var rect  = P3dHelper.Reserve(); rect.xMin += 10;
								var mat   = scene.GetMat(matId);

								if (GUI.Button(rect, mat != null ? mat.Name : "", EditorStyles.popup) == true)
								{
									var menu = new GenericMenu();

									for (var k = 0; k < scene.Mats.Count; k++)
									{
										var setObj = obj;
										var setIdx = j;
										var setMat = scene.Mats[k];

										menu.AddItem(new GUIContent(setMat.Name), setMat == mat, () => setObj.MatIds[setIdx] = setMat.Id);
									}
										
									var remObj = obj;
									var remIdx = j;

									menu.AddSeparator("");
									menu.AddItem(new GUIContent("Remove"), false, () => remObj.MatIds.RemoveAt(remIdx));

									menu.DropDown(rect);
								}
							}
						}
						else
						{
							EditorGUILayout.LabelField(obj.Name);
						}
					P3dHelper.EndLabelWidth();
				EditorGUILayout.EndVertical();

				if (Event.current.type == EventType.MouseDown && objRect.Contains(Event.current.mousePosition) == true)
				{
					currentObj = obj; P3dHelper.ClearControl();
				}
			}
		}
	}
}
#endif