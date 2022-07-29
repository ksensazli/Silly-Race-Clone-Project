#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		private void UpdateClonersPanel()
		{
			for (var i = 0; i < scene.Cloners.Count; i++)
			{
				EditorGUILayout.Space();

				var cloner  = scene.Cloners[i];
				var matRect = 
				EditorGUILayout.BeginVertical(GetSelectableStyle(currentCloners.Contains(cloner), true));
					P3dHelper.BeginLabelWidth(60.0f);
						if (currentCloners.Contains(cloner) == true)
						{
							EditorGUILayout.BeginHorizontal();
								P3dHelper.BeginColor(scene.MatNameValid(cloner.Name) == false);
									cloner.Name = EditorGUILayout.TextField(cloner.Name);
								P3dHelper.EndColor();
								if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)) == true && EditorUtility.DisplayDialog("Are you sure?", "This will delete the current cloner from the paint scene.", "Delete") == true)
								{
									scene.Cloners.RemoveAt(i--); P3dHelper.ClearControl();
								}
							EditorGUILayout.EndHorizontal();
							cloner.Position = EditorGUILayout.Vector3Field("Position", cloner.Position);
							cloner.Euler    = EditorGUILayout.Vector3Field("Euler", cloner.Euler);
							cloner.Flip     = EditorGUILayout.Toggle("Flip", cloner.Flip);
						}
						else
						{
							EditorGUILayout.LabelField(cloner.Name);
						}
					P3dHelper.EndLabelWidth();
				EditorGUILayout.EndVertical();

				if (Event.current.type == EventType.MouseDown && matRect.Contains(Event.current.mousePosition) == true)
				{
					if (currentCloners.Remove(cloner) == false)
					{
						currentCloners.Add(cloner);
					}
					
					P3dHelper.ClearControl();
				}
			}

			if (GUILayout.Button("Add Cloner") == true)
			{
				currentCloners.Add(scene.AddCloner("New Cloner", Vector3.zero, Vector3.zero)); P3dHelper.ClearControl();
			}
		}
	}
}
#endif