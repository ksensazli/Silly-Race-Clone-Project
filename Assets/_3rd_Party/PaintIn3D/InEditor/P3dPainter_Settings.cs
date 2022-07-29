#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		partial class SettingsData
		{
			public string ExportFormat = "{SCENE}_{MATERIAL}_{TEXTURE}";

			public int ThumbnailSize = 64;

			public P3dGroup ColorModifies = new P3dGroup(-199); // Albedo
		}

		private void UpdateSettingsPanel()
		{
			P3dHelper.BeginLabelWidth(100);
				settings.Environment = (ReflectionProbe)EditorGUILayout.ObjectField("Environment", settings.Environment, typeof(ReflectionProbe), true);
				settings.DefaultTextureSize = EditorGUILayout.IntField("Default Size", settings.DefaultTextureSize);
				settings.TurnSensitivity = EditorGUILayout.FloatField("Turn Sensitivity", settings.TurnSensitivity);
				settings.ZoomSensitivity = EditorGUILayout.FloatField("Zoom Sensitivity", settings.ZoomSensitivity);
				settings.UndoMegabytes = EditorGUILayout.IntField("Undo Megabytes", settings.UndoMegabytes);
				settings.ExportFormat = EditorGUILayout.TextField("Export Format", settings.ExportFormat);
				settings.ThumbnailSize = EditorGUILayout.IntSlider("Thumbnail Size", settings.ThumbnailSize, 32, 256);
				settings.ColorModifies = EditorGUILayout.IntField(new GUIContent("Color Modifies", "The texture group that will be tinted by the current color (e.g. Albedo = -199)."), settings.ColorModifies);
			P3dHelper.EndLabelWidth();
		}
	}
}
#endif