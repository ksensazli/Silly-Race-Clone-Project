#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		private void UpdateLayersPanel()
		{
			GUILayout.FlexibleSpace();

			for (var i = scene.Layers.Count - 1; i >= 0; i--)
			{
				if (i < scene.Layers.Count - 1) EditorGUILayout.Space();

				var layer     = scene.Layers[i];
				var layerRect = 
				EditorGUILayout.BeginVertical(GetSelectableStyle(layer == currentLayer, true));
					P3dHelper.BeginLabelWidth(60.0f);
						GUILayout.BeginHorizontal();
							if (layer == currentLayer)
							{
								EditorGUILayout.BeginHorizontal();
									layer.Name = EditorGUILayout.TextField(layer.Name);
									if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)) == true && EditorUtility.DisplayDialog("Are you sure?", "This will delete the current layer from the paint window.", "Delete") == true)
									{
										scene.RemoveLayer(layer); P3dHelper.ClearControl();
									}
								EditorGUILayout.EndHorizontal();
							}
							else
							{
								GUILayout.Label(layer.Name);
							}
						GUILayout.EndHorizontal();

						if (layer == currentLayer)
						{
							EditorGUI.BeginChangeCheck();

							layer.Opacity = EditorGUILayout.Slider("Opacity", layer.Opacity, 0.0f, 1.0f);

							if (EditorGUI.EndChangeCheck() == true)
							{
								UpdatePaintedMats();
							}

							foreach (var image in layer.Images)
							{
								EditorGUILayout.ObjectField(image.Current, typeof(Texture), true);
							}
						}
					P3dHelper.EndLabelWidth();
				GUILayout.EndVertical();

				if (Event.current.type == EventType.MouseDown && layerRect.Contains(Event.current.mousePosition) == true)
				{
					currentLayer = layer; P3dHelper.ClearControl();
				}
			}

			if (GUILayout.Button("Add Layer") == true)
			{
				currentLayer = scene.AddLayer(); P3dHelper.ClearControl();
			}
		}
	}
}
#endif