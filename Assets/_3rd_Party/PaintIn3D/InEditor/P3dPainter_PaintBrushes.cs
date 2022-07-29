#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		class BrushCategory
		{
			public bool Expand = true;

			public List<P3dPaintBrush> PaintBrushes = new List<P3dPaintBrush>();

			public static int Compare(P3dPaintBrush a, P3dPaintBrush b)
			{
				return a.name.CompareTo(b.name);
			}
		}

		[SerializeField] private Vector2 paintBrushesScrollPosition;

		private static Dictionary<string, BrushCategory> brushCategories = new Dictionary<string, BrushCategory>();

		private void UpdatePaintBrushesBrowser(float width)
		{
			var removeCategory = default(string);

			foreach (var brushCategory in brushCategories.Values)
			{
				brushCategory.PaintBrushes.Clear();
			}

			for (var i = P3dPaintBrush.CachedInstances.Count - 1; i >= 0; i--)
			{
				var paintBrush = P3dPaintBrush.CachedInstances[i];

				if (paintBrush != null)
				{
					var category      = paintBrush.Category ?? "";
					var brushCategory = default(BrushCategory);

					if (brushCategories.TryGetValue(category, out brushCategory) == false)
					{
						brushCategory = new BrushCategory();

						brushCategories.Add(category, brushCategory);
					}

					brushCategory.PaintBrushes.Add(paintBrush);
				}
				else
				{
					P3dPaintBrush.CachedInstances.RemoveAt(i);
				}
			}

			foreach (var pair in brushCategories)
			{
				var category      = pair.Key;
				var brushCategory = pair.Value;

				if (brushCategory.PaintBrushes.Count > 0)
				{
					brushCategory.Expand = EditorGUILayout.Foldout(brushCategory.Expand, category);

					if (brushCategory.Expand == true)
					{
						brushCategory.PaintBrushes.Sort(BrushCategory.Compare);

						DrawBrushes(brushCategory.PaintBrushes, width);
					}
				}
				else
				{
					removeCategory = category;
				}
			}

			if (removeCategory != null)
			{
				brushCategories.Remove(removeCategory);
			}
		}

		private void DrawBrushes(List<P3dPaintBrush> brushes, float width)
		{
			var columns  = Mathf.FloorToInt((width - 10) / settings.ThumbnailSize);
			var rowIndex = 0;

			for (var i = 0; i < brushes.Count; i++)
			{
				var brush = brushes[i];

				if (rowIndex == 0)
				{
					GUILayout.BeginHorizontal();
				}

				var rectO = 
				EditorGUILayout.BeginHorizontal(GUILayout.Width(settings.ThumbnailSize), GUILayout.Height(settings.ThumbnailSize));
					brush.Data.Draw(rectO);
					GUILayout.Label(new GUIContent(default(Texture), brush.name + "\n\nLeft Mouse = Select.\nRight Mouse = Find Asset."), GetSelectableStyle(false, false), GUILayout.Width(settings.ThumbnailSize), GUILayout.Height(settings.ThumbnailSize));
				EditorGUILayout.EndHorizontal();

				if (Event.current.type == EventType.MouseDown && rectO.Contains(Event.current.mousePosition) == true)
				{
					if (Event.current.button == 0)
					{
						paintBrushData     = brush.Data;
						nextSelectingBrush = false;
					}
					else
					{
						Selection.activeObject = brush; EditorGUIUtility.PingObject(brush);
					}
				}

				if (++rowIndex >= columns || i == brushes.Count - 1)
				{
					GUILayout.EndHorizontal(); rowIndex = 0;
				}
			}
		}
	}
}
#endif