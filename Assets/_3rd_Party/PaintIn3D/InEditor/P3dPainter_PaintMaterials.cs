#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		class MaterialCategory
		{
			public bool Expand = true;

			public List<P3dPaintMaterial> PaintMaterials = new List<P3dPaintMaterial>();

			public static int Compare(P3dPaintMaterial a, P3dPaintMaterial b)
			{
				return a.name.CompareTo(b.name);
			}
		}

		[SerializeField] private Vector2 paintMaterialsScrollPosition;

		private static Dictionary<string, MaterialCategory> materialCategories = new Dictionary<string, MaterialCategory>();

		private void UpdatePaintMaterialsBrowser(float width)
		{
			var removeCategory = default(string);

			foreach (var materialCategory in materialCategories.Values)
			{
				materialCategory.PaintMaterials.Clear();
			}

			for (var i = P3dPaintMaterial.CachedInstances.Count - 1; i >= 0; i--)
			{
				var paintMaterial = P3dPaintMaterial.CachedInstances[i];

				if (paintMaterial != null)
				{
					var category         = paintMaterial.Category ?? "";
					var materialCategory = default(MaterialCategory);

					if (materialCategories.TryGetValue(category, out materialCategory) == false)
					{
						materialCategory = new MaterialCategory();

						materialCategories.Add(category, materialCategory);
					}

					materialCategory.PaintMaterials.Add(paintMaterial);
				}
				else
				{
					P3dPaintMaterial.CachedInstances.RemoveAt(i);
				}
			}

			foreach (var pair in materialCategories)
			{
				var category         = pair.Key;
				var materialCategory = pair.Value;

				if (materialCategory.PaintMaterials.Count > 0)
				{
					materialCategory.Expand = EditorGUILayout.Foldout(materialCategory.Expand, category);

					if (materialCategory.Expand == true)
					{
						materialCategory.PaintMaterials.Sort(MaterialCategory.Compare);

						DrawMaterials(materialCategory.PaintMaterials, width);
					}
				}
				else
				{
					removeCategory = category;
				}
			}

			if (removeCategory != null)
			{
				materialCategories.Remove(removeCategory);
			}

			if (currentPaintMaterial == null)
			{
				EditorGUILayout.HelpBox("You must select a paint material before you can paint.", MessageType.Info);
			}
		}

		private void DrawMaterials(List<P3dPaintMaterial> materials, float width)
		{
			var columns  = Mathf.FloorToInt((width - 10) / settings.ThumbnailSize);
			var rowIndex = 0;

			for (var i = 0; i < materials.Count; i++)
			{
				var material = materials[i];

				if (rowIndex == 0)
				{
					GUILayout.BeginHorizontal();
				}

				var rectO = 
				EditorGUILayout.BeginHorizontal(GUILayout.Width(settings.ThumbnailSize), GUILayout.Height(settings.ThumbnailSize));
					if (material.Thumbnail != null)
					{
						GUI.DrawTexture(rectO, material.Thumbnail);
					}
					GUILayout.Label(new GUIContent(default(Texture), material.name + "\n\nLeft Mouse = Select.\nRight Mouse = Find Asset."), GetSelectableStyle(false, false), GUILayout.Width(settings.ThumbnailSize), GUILayout.Height(settings.ThumbnailSize));
				EditorGUILayout.EndHorizontal();

				if (Event.current.type == EventType.MouseDown && rectO.Contains(Event.current.mousePosition) == true)
				{
					if (Event.current.button == 0)
					{
						currentPaintMaterial  = material;
						nextSelectingMaterial = false;
					}
					else
					{
						Selection.activeObject = material; EditorGUIUtility.PingObject(material);
					}
				}

				if (++rowIndex >= columns || i == materials.Count - 1)
				{
					GUILayout.EndHorizontal(); rowIndex = 0;
				}
			}
		}
	}
}
#endif