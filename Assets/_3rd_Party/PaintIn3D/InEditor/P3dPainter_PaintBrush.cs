#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		enum ModeType
		{
			Airbrush = 0,
			Point    = 10,
			//Brush    = 20,
			Triangle = 30
		}

		partial class SettingsData
		{
			public ModeType PaintMode;
			public float    PaintScale   = 1.0f;
			public float    PaintOpacity = 1.0f;
			public float    PaintAngle;
		}

		[SerializeField] private Vector2 dynamicsScrollPosition;

		private bool selectingBrush;

		private bool nextSelectingBrush;

		private void UpdateDynamicsPanel(float width)
		{
			if (nextSelectingBrush != selectingBrush && Event.current.type == EventType.Layout)
			{
				selectingBrush = nextSelectingBrush;
			}

			EditorGUILayout.BeginVertical(GUILayout.Width(width));
				EditorGUILayout.Separator();
					DrawBrushTop(width);
				EditorGUILayout.Separator();

				if (selectingBrush == true)
				{
					paintBrushesScrollPosition = GUILayout.BeginScrollView(paintBrushesScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
						UpdatePaintBrushesBrowser(width);
					GUILayout.EndScrollView();
				}
				else
				{
					dynamicsScrollPosition = GUILayout.BeginScrollView(dynamicsScrollPosition, GUILayout.Width(200));
						P3dHelper.BeginLabelWidth(50);
							UpdateDynamicsBrushPanel();
						P3dHelper.EndLabelWidth();
					GUILayout.EndScrollView();
				}
			EditorGUILayout.EndVertical();
		}

		private void DrawBrushTop(float width)
		{
			EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
					var rectL = 
					EditorGUILayout.BeginHorizontal(GUILayout.Width(settings.ThumbnailSize), GUILayout.Height(settings.ThumbnailSize));
						paintBrushData.Draw(rectL);
						GUILayout.Label(new GUIContent(default(Texture), "Click to choose a paint brush."), GetSelectableStyle(selectingBrush, false), GUILayout.Width(settings.ThumbnailSize), GUILayout.Height(settings.ThumbnailSize));
					EditorGUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			if (Event.current.type == EventType.MouseDown && rectL.Contains(Event.current.mousePosition) == true)
			{
				nextSelectingBrush = !selectingBrush;
			}
		}

		private void UpdateDynamicsBrushPanel()
		{
			P3dHelper.BeginLabelWidth(50);
				settings.PaintMode   = (ModeType)EditorGUILayout.EnumPopup("Mode", settings.PaintMode);
				settings.PaintScale   = LogSlider("Scale", settings.PaintScale, -4, 4);
				settings.PaintOpacity = EditorGUILayout.Slider("Opacity", settings.PaintOpacity, 0.0f, 1.0f);
				settings.PaintAngle   = EditorGUILayout.Slider("Angle", settings.PaintAngle, -180.0f, 180.0f);

				EditorGUILayout.Separator();

				GUILayout.FlexibleSpace();

				paintBrushData.Modifiers.DrawEditorLayout(false, "Angle", "Opacity", "Position", "Radius");
			P3dHelper.EndLabelWidth();
		}
	}
}
#endif