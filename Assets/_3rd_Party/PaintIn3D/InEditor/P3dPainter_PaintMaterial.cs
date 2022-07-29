#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		partial class SettingsData
		{
			public float PaintTile = 1.0f;

			public float PaintNormalFront = 2.0f;

			public float PaintNormalBack = 0.0f;

			public float PaintNormalFade = 0.01f;

			public Color PaintColor = Color.white;
		}

		private bool selectingMaterial;

		private bool nextSelectingMaterial;

		private static HashSet<P3dGroup> layerGroups = new HashSet<P3dGroup>();

		private static HashSet<P3dGroup> excludedGroups = new HashSet<P3dGroup>();

		private static HashSet<P3dGroup> isolatedGroups = new HashSet<P3dGroup>();

		private void UpdatePaintPanel(float width)
		{
			if (nextSelectingMaterial != selectingMaterial && Event.current.type == EventType.Layout)
			{
				selectingMaterial = nextSelectingMaterial;
			}

			EditorGUILayout.BeginVertical(GUILayout.Width(width));
				EditorGUILayout.Separator();	
					DrawMaterialTop(width);
				EditorGUILayout.Separator();

				if (selectingMaterial == true)
				{
					paintMaterialsScrollPosition = GUILayout.BeginScrollView(paintMaterialsScrollPosition, GUILayout.Width(width));
						UpdatePaintMaterialsBrowser(width);
					GUILayout.EndScrollView();
				}
				else
				{
					paintMaterialsScrollPosition = GUILayout.BeginScrollView(paintMaterialsScrollPosition, GUILayout.Width(width));
						P3dHelper.BeginLabelWidth(50);
							settings.PaintTile = LogSlider("Tiling", settings.PaintTile , -2, 4);

							var color = settings.PaintColor;

							color   = EditorGUILayout.ColorField("Color", color);
							color.r = Slider("Red", color.r, 0.0f, 1.0f);
							color.g = Slider("Green", color.g, 0.0f, 1.0f);
							color.b = Slider("Blue", color.b, 0.0f, 1.0f);
							color.a = Slider("Alpha", color.a, 0.0f, 1.0f);

							float h, s, v; Color.RGBToHSV(color, out h, out s, out v);

							h = Slider("Hue"       , h, 0.0f, 1.0f);
							s = Slider("Saturation", s, 0.0f, 1.0f);
							v = Slider("Value"     , v, 0.0f, 1.0f);

							var newColor = Color.HSVToRGB(h, s, v);

							color.r = newColor.r;
							color.g = newColor.g;
							color.b = newColor.b;

							settings.PaintColor = color;

							EditorGUILayout.Separator();
							EditorGUILayout.LabelField("Normal", EditorStyles.boldLabel);
							settings.PaintNormalFront = EditorGUILayout.Slider("Front", settings.PaintNormalFront, 0.0f, 2.0f);
							settings.PaintNormalBack = EditorGUILayout.Slider("Back", settings.PaintNormalBack, 0.0f, 2.0f);
							settings.PaintNormalFade = EditorGUILayout.Slider("Fade", settings.PaintNormalFade, 0.001f, 0.5f);
						P3dHelper.EndLabelWidth();

						GUILayout.FlexibleSpace();

						P3dHelper.BeginLabelWidth(50);
							CompileLayerGroups();
							DrawLayerGroups();
						P3dHelper.EndLabelWidth();
					GUILayout.EndScrollView();
				}
			EditorGUILayout.EndVertical();
		}

		private void DrawMaterialTop(float width)
		{
			EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
					var rectL = 
					EditorGUILayout.BeginHorizontal(GUILayout.Width(settings.ThumbnailSize), GUILayout.Height(settings.ThumbnailSize));
						P3dPaintMaterial.Draw(currentPaintMaterial, rectL);
						GUILayout.Label(new GUIContent(default(Texture), "Click to choose a paint material."), GetSelectableStyle(selectingMaterial, false), GUILayout.Width(settings.ThumbnailSize), GUILayout.Height(settings.ThumbnailSize));
					EditorGUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			if (currentPaintMaterial == null && selectingMaterial == false)
			{
				EditorGUILayout.HelpBox("Click the box above to choose a paint material.", MessageType.Warning);
			}

			if (Event.current.type == EventType.MouseDown && rectL.Contains(Event.current.mousePosition) == true)
			{
				nextSelectingMaterial = !selectingMaterial;
			}
		}

		private void DrawLayerGroups()
		{
			var paintable = false;

			if (currentPaintMaterial != null)
			{
				foreach (var slot in currentPaintMaterial.Slots)
				{
					var layerGroup = slot.Group;

					EditorGUILayout.BeginVertical(EditorStyles.helpBox);
						EditorGUILayout.LabelField(P3dGroupData.GetGroupName(layerGroup, false));
						var used    = !excludedGroups.Contains(layerGroup);
						var isolate = isolatedGroups.Contains(layerGroup);

						EditorGUI.BeginDisabledGroup(isolate == false && isolatedGroups.Count > 0);
							used = EditorGUILayout.Toggle("Paint", used);
						EditorGUI.EndDisabledGroup();
						isolate = EditorGUILayout.Toggle("Isolate", isolate);

						if (used == true)
						{
							excludedGroups.Remove(layerGroup);

							paintable = true;
						}
						else
						{
							excludedGroups.Add(layerGroup);
						}

						if (isolate == true)
						{
							isolatedGroups.Clear();
							isolatedGroups.Add(layerGroup);

							paintable = true;
						}
						else
						{
							isolatedGroups.Remove(layerGroup);
						}
					EditorGUILayout.EndVertical();
				}
			}

			if (layerGroups.Count == 0)
			{
				EditorGUILayout.HelpBox("You haven't added any paintable objects and/or materials in the Scene tab.", MessageType.Warning);
			}
			else if (paintable == false)
			{
				EditorGUILayout.HelpBox("You must enable painting for at least one texture.", MessageType.Warning);
			}
		}

		private void CompileLayerGroups()
		{
			layerGroups.Clear();

			foreach (var mat in scene.Mats)
			{
				if (mat.Template != null)
				{
					foreach (var slot in mat.Template.Slots)
					{
						layerGroups.Add(slot.WriteR.SourceGroup);
						layerGroups.Add(slot.WriteG.SourceGroup);
						layerGroups.Add(slot.WriteB.SourceGroup);
						layerGroups.Add(slot.WriteA.SourceGroup);
					}
				}
			}

			isolatedGroups.RemoveWhere(g => layerGroups.Contains(g) == false);
		}
	}
}
#endif