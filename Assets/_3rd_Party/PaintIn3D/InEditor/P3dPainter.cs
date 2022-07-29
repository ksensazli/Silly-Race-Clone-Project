#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace PaintIn3D
{
	public partial class P3dPainter : P3dEditorWindow
	{
		private P3dScene scene;

		private PreviewRenderUtility previewUtil;
		private PreviewRenderUtility thumbnailUtil;

		private static GUIStyle selectableStyleA;
		private static GUIStyle selectableStyleB;
		private static GUIStyle selectableStyleC;

		enum StateType
		{
			Scene,
			Paint,
			Layer,
			Config
		}

		enum SideType
		{
			LeftPanel,
			RightPanel,
			VerticalSplit
		}

		[System.Serializable]
		class LightData
		{
			public float Pitch;
			public float Yaw;
			public float Intensity;
			public Color Color;
		}

		[System.Serializable]
		partial class SettingsData
		{
			public ReflectionProbe Environment;

			public int DefaultTextureSize = 1024;

			public float TurnSensitivity = 0.2f;

			public float ZoomSensitivity = 0.1f;

			public int UndoMegabytes = 1024;

			public LightData Light0 = new LightData { Pitch = 45.0f, Yaw = 45.0f, Intensity = 1.5f, Color = Color.white };

			public LightData Light1 = new LightData { Pitch = -45.0f, Yaw = 180.0f, Intensity = 1.0f, Color = Color.white };
		}

		class StateData
		{
			public List<P3dScene.Layer> Layers = new List<P3dScene.Layer>();

			public List<P3dScene.Mat> Mats = new List<P3dScene.Mat>();

			public List<P3dScene.Obj> Objs = new List<P3dScene.Obj>();
		}

		private Vector3 cameraOrigin;
		private float   cameraYaw;
		private float   cameraPitch;
		private float   cameraDistance = 10.0f;

		private SettingsData settings = new SettingsData();

		[System.NonSerialized]
		private List<StateData> states = new List<StateData>();

		[System.NonSerialized]
		private int stateIndex;

		private static int paintIndex;

		private int dragId;

		private int dragHandle;

		private StateType currentState;

		private P3dPaintMaterial currentPaintMaterial;

		private P3dPaintBrush.SharedData paintBrushData = new P3dPaintBrush.SharedData();

		private P3dScene.Mat currentMat;

		private P3dScene.Obj currentObj;

		private P3dScene.Layer currentLayer;

		private List<P3dScene.Cloner> currentCloners = new List<P3dScene.Cloner>();

		private static MaterialPropertyBlock paintProperties;

		private Rect fullRect = new Rect(0, 0, 100, 100);

		public P3dScene Scene
		{
			set
			{
				scene = value;
			}

			get
			{
				return scene;
			}
		}

		private static Texture2D LoadTempTexture(string base64)
		{
			var tex  = new Texture2D(1, 1);
			var data = System.Convert.FromBase64String(base64);

			tex.LoadImage(data);

			return tex;
		}

		private GUIStyle GetSelectableStyle(bool selected, bool pad)
		{
			if (selectableStyleA == null || selectableStyleA.normal.background == null)
			{
				selectableStyleA = new GUIStyle(); selectableStyleA.border = new RectOffset(4,4,4,4); selectableStyleA.normal.background = LoadTempTexture("iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAASUlEQVQYGWN0iL73nwEPYIHKNeJQUw9TAJI/gKbIAcRnQhbcv0TxAAgji6EoQJaAsZGtYHCMue8Ak4DRyAowJEGKYArqYTrQaQBpfAuV0+TyawAAAABJRU5ErkJggg==");
			}

			if (selectableStyleB == null || selectableStyleB.normal.background == null)
			{
				selectableStyleB = new GUIStyle(); selectableStyleB.border = new RectOffset(4,4,4,4); selectableStyleB.normal.background = LoadTempTexture("iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAARElEQVQYGWNkYGBwAGKcgAUq8wCHCgWYApD8BzRFAiA+E5ogSBGKQnQFaOoZGJCtAEmCjUVWhawAQxKkEKZAAVkXMhsAA6sEekpg61oAAAAASUVORK5CYII=");
			}

			if (selectableStyleC == null)
			{
				selectableStyleC = new GUIStyle(selectableStyleA); selectableStyleC.padding = new RectOffset(2,2,4,4);
			}

			if (selected == true)
			{
				return pad == true ? selectableStyleC : selectableStyleA;
			}

			return selectableStyleB;
		}

		private void RemoveState(int index)
		{
			var state = states[index];

			states.RemoveAt(index);
		}

		private void AddState()
		{
			var state = new StateData();

			foreach (var obj in scene.Objs)
			{
				state.Objs.Add(obj.Clone);
			}

			foreach (var mat in scene.Mats)
			{
				state.Mats.Add(mat.Clone);
			}

			foreach (var layer in scene.Layers)
			{
				state.Layers.Add(layer.Clone);
			}

			states.Add(state);
		}

		private void StoreState()
		{
			if (stateIndex != states.Count - 1)
			{
				for (var i = states.Count - 1; i >= stateIndex; i--)
				{
					RemoveState(i);
				}

				var totalMegabytes = 0;

				for (var i = states.Count - 1; i >= 0; i--)
				{
					if (totalMegabytes > settings.UndoMegabytes)
					{
						RemoveState(i);
					}
					else
					{
						totalMegabytes += GetMegabytes(states[i].Layers);
					}
				}

				AddState();
			}

			stateIndex = states.Count;
		}

		private bool CanUndo
		{
			get
			{
				return stateIndex > 0;
			}
		}

		private bool CanRedo
		{
			get
			{
				return stateIndex < states.Count - 1;
			}
		}

		private void Undo()
		{
			if (CanUndo == true)
			{
				if (stateIndex == states.Count)
				{
					AddState();
				}

				LoadState(states[--stateIndex]);
			}
		}

		private void Redo()
		{
			if (CanRedo == true)
			{
				LoadState(states[++stateIndex]);
			}
		}

		private void LoadState(StateData state)
		{
			scene.Clear();

			scene.Objs   = state.Objs;
			scene.Mats   = state.Mats;
			scene.Layers = state.Layers;

			currentLayer = null;
			currentMat   = null;
			currentObj   = null;

			if (scene.Layers.Count > 0)
			{
				currentLayer = scene.Layers[0];
			}

			UpdatePaintedMats();
		}

		[MenuItem("Window/Paint in 3D (NEW)")]
		public static void OpenWindow()
		{
			GetWindow();
		}

		public static P3dPainter GetWindow()
		{
			return GetWindow<P3dPainter>("Paint in 3D", true);
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (settings == null)
			{
				settings = new SettingsData();
			}

			/*
			if (currentPaintBrush == null)
			{
				var guids = AssetDatabase.FindAssets("t:P3dPaintBrush Circle A");

				if (guids.Length > 0)
				{
					currentPaintBrush = AssetDatabase.LoadAssetAtPath<P3dPaintBrush>(AssetDatabase.GUIDToAssetPath(guids[0]));
				}
			}
			*/

			EditorJsonUtility.FromJsonOverwrite(EditorPrefs.GetString("P3dPainter.Settings"), settings);

			autoRepaintOnSceneChange = true;

			previewUtil = new PreviewRenderUtility(true);

			previewUtil.camera.clearFlags    = CameraClearFlags.Skybox;
			previewUtil.camera.nearClipPlane = 0.01f;
			previewUtil.camera.farClipPlane  = 10000.0f;
			previewUtil.camera.fieldOfView   = 60.0f;

			thumbnailUtil = new PreviewRenderUtility(false);

			thumbnailUtil.camera.clearFlags       = CameraClearFlags.Color;
			thumbnailUtil.camera.backgroundColor  = Color.clear;
			thumbnailUtil.camera.nearClipPlane    = 0.01f;
			thumbnailUtil.camera.farClipPlane     = 1000.0f;
			thumbnailUtil.camera.fieldOfView      = 15.0f;

			thumbnailUtil.camera.transform.position = Vector3.back * 4.0f;
			thumbnailUtil.camera.transform.rotation = Quaternion.identity;

			thumbnailUtil.lights[0].transform.rotation = Quaternion.Euler( 30.0f, 320.0f, 0.0f);
			thumbnailUtil.lights[1].transform.rotation = Quaternion.Euler(330.0f,  85.0f, 0.0f);
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			EditorPrefs.SetString("P3dPainter.Settings", EditorJsonUtility.ToJson(settings));

			previewUtil.Cleanup();
			thumbnailUtil.Cleanup();
		}

		private void DrawNextPaint()
		{
			if (P3dPaintMaterial.CachedInstances.Count > 0)
			{
				if (paintIndex >= P3dPaintMaterial.CachedInstances.Count)
				{
					paintIndex = 0;
				}

				var paint = P3dPaintMaterial.CachedInstances[paintIndex++];

				DestroyImmediate(paint.Thumbnail);
				paint.Thumbnail = null;

				if (paint != null && paint.Thumbnail == null && paint.Material != null)
				{
					var tempTextures = new List<RenderTexture>();

					if (paintProperties == null)
					{
						paintProperties = new MaterialPropertyBlock();
					}
					else
					{
						paintProperties.Clear();
					}

					if (paint.Template != null)
					{
						foreach (var slot in paint.Template.Slots)
						{
							var slotR = paint.GetSlot(slot.WriteR.SourceGroup);
							var slotG = paint.GetSlot(slot.WriteG.SourceGroup);
							var slotB = paint.GetSlot(slot.WriteB.SourceGroup);
							var slotA = paint.GetSlot(slot.WriteA.SourceGroup);

							if (slotR != null || slotG != null || slotB != null || slotA != null)
							{
								var tempTexture = P3dHelper.GetRenderTexture(new RenderTextureDescriptor(1024, 1024, RenderTextureFormat.ARGB32, 0));

								tempTextures.Add(tempTexture);

								var textureR = P3dHelper.GetRenderTexture(tempTexture.descriptor);
								var textureG = P3dHelper.GetRenderTexture(tempTexture.descriptor);
								var textureB = P3dHelper.GetRenderTexture(tempTexture.descriptor);
								var textureA = P3dHelper.GetRenderTexture(tempTexture.descriptor);

								WriteThumbnailTex(textureR, slotR, slot.WriteR);
								WriteThumbnailTex(textureG, slotG, slot.WriteG);
								WriteThumbnailTex(textureB, slotB, slot.WriteB);
								WriteThumbnailTex(textureA, slotA, slot.WriteA);

								var channelR = P3dHelper.IndexToVector((int)slot.WriteR.SourceChannel);
								var channelG = P3dHelper.IndexToVector((int)slot.WriteG.SourceChannel);
								var channelB = P3dHelper.IndexToVector((int)slot.WriteB.SourceChannel);
								var channelA = P3dHelper.IndexToVector((int)slot.WriteA.SourceChannel);

								P3dCommandReplaceChannels.Blit(tempTexture, textureR, textureG, textureB, textureA, channelR, channelG, channelB, channelA);

								P3dHelper.ReleaseRenderTexture(textureR);
								P3dHelper.ReleaseRenderTexture(textureG);
								P3dHelper.ReleaseRenderTexture(textureB);
								P3dHelper.ReleaseRenderTexture(textureA);

								paintProperties.SetTexture(slot.Name, tempTexture);
							}
						}
					}

					BeginPreview(thumbnailUtil, new Rect(0, 0, settings.ThumbnailSize, settings.ThumbnailSize));

					var probeAnchor = default(Transform);
					var probeExists = false;

					if (settings.Environment != null)
					{
						probeAnchor = settings.Environment.transform;
						probeExists = true;
					}

					switch (paint.Style)
					{
						case P3dPaintMaterial.StyleType.Seamless:
						{
							thumbnailUtil.DrawMesh(P3dHelper.GetSphereMesh(), Matrix4x4.identity, paint.Material, 0, paintProperties, probeAnchor, probeExists);
						}
						break;

						case P3dPaintMaterial.StyleType.Decal:
						{
							thumbnailUtil.DrawMesh(P3dHelper.GetQuadMesh(), Matrix4x4.identity, paint.Material, 0, paintProperties, probeAnchor, probeExists);
						}
						break;
					}

					paint.Thumbnail = P3dHelper.GetReadableCopy(EndPreview(thumbnailUtil));

					foreach (var tempTexture in tempTextures)
					{
						P3dHelper.ReleaseRenderTexture(tempTexture);
					}
				}
			}
		}

		private void WriteThumbnailTex(RenderTexture texture, P3dPaintMaterial.Slot paintSlot, P3dShaderTemplate.Write write)
		{
			if (paintSlot != null)
			{
				P3dCommandReplace.Blit(texture, paintSlot.Texture, paintSlot.Color);
			}
			else
			{
				var group = P3dGroupData.GetGroupData(write.SourceGroup);

				if (group != null)
				{
					P3dCommandReplace.Blit(texture, group.DefaultTexture, group.DefaultColor);
				}
				else
				{
					P3dCommandReplace.Blit(texture, default(Texture), default(Color));
				}
			}
		}

		private void HandleSave()
		{
			scene.Save();

			EditorUtility.SetDirty(scene);
		}

		private void HandleSaveAs()
		{
			var path = EditorUtility.SaveFilePanelInProject("Save Paint Scene", "MyPaintScene", "asset", "test");

			if (string.IsNullOrEmpty(path) == false)
			{
				AssetDatabase.CreateAsset(scene, path);

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.FocusProjectWindow();

				Selection.activeObject = scene;

				HandleSave();
			}
		}

		private void HandleExport()
		{
			var basePath = AssetDatabase.GetAssetPath(scene);

			if (string.IsNullOrEmpty(basePath) == true)
			{
				basePath = "Assets/";
			}
			else
			{
				basePath = System.IO.Path.GetDirectoryName(basePath) + "/";
			}

			foreach (var mat in scene.Mats)
			{
				if (mat.Template != null)
				{
					foreach (var matSlot in mat.Slots)
					{
						var sceneSlot = mat.Template.Slots.FirstOrDefault(s => s.Name == matSlot.Name);

						if (sceneSlot != null && matSlot.Texture != null)
						{
							var path    = basePath + settings.ExportFormat.Replace("{SCENE}", scene.name).Replace("{MATERIAL}", mat.Name).Replace("{TEXTURE}", sceneSlot.GetAlias()) + ".png";
							var texture = P3dHelper.GetReadableCopy(matSlot.Texture);
							var data    = texture.EncodeToPNG();

							DestroyImmediate(texture);

							System.IO.File.WriteAllBytes(path, data);

							Debug.Log("EXPORT " + path);
						}
					}
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		[SerializeField] private Vector2 objectsScrollPosition;
		[SerializeField] private Vector2 materialsScrollPosition;
		[SerializeField] private Vector2 repeatersScrollPosition;
		[SerializeField] private Vector2 sceneScrollPosition;
		[SerializeField] private Vector2 layersScrollPosition;
		[SerializeField] private Vector2 settingsScrollPosition;

		protected override void OnInspector()
		{
			DrawNextPaint();

			if (scene == null)
			{
				scene = CreateInstance<P3dScene>();

				scene.name = "Temp";

				scene.AddLayer();

				scene.AddCloner("Mirror X", Vector3.zero, new Vector3( 0.0f, 90.0f, 0.0f));
				scene.AddCloner("Mirror Y", Vector3.zero, new Vector3(90.0f,  0.0f, 0.0f));
				scene.AddCloner("Mirror Z", Vector3.zero, new Vector3( 0.0f,  0.0f, 0.0f));
			}

			if (previewDrawn == true && Event.current.type == EventType.Repaint)
			{
				previewDrawn = false;

				foreach (var layer in scene.Layers)
				{
					foreach (var images in layer.Images)
					{
						if (images.Preview != null)
						{
							images.Preview = P3dHelper.ReleaseRenderTexture(images.Preview);
						}
					}
				}

				pendingUpdate = true;
			}

			if (scene.Layers.Contains(currentLayer) == false)
			{
				currentLayer = null;

				if (scene.Layers.Count > 0)
				{
					currentLayer = scene.Layers[scene.Layers.Count - 1];
				}
			}

			if (scene.Mats.Contains(currentMat) == false)
			{
				currentMat = null;

				if (scene.Mats.Count > 0)
				{
					currentMat = scene.Mats[scene.Mats.Count - 1];
				}
			}

			if (scene.Objs.Contains(currentObj) == false)
			{
				currentObj = null;

				if (scene.Objs.Count > 0)
				{
					currentObj = scene.Objs[scene.Objs.Count - 1];
				}
			}

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				EditorGUI.BeginDisabledGroup(CanUndo == false);
					if (GUILayout.Button(new GUIContent("◄", "Undo"), EditorStyles.toolbarButton, GUILayout.Width(20)) == true)
					{
						Undo();
					}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(CanRedo == false);
					if (GUILayout.Button(new GUIContent("►", "Redo"), EditorStyles.toolbarButton, GUILayout.Width(20)) == true)
					{
						Redo();
					}
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Toggle(currentState == StateType.Scene, "Scene", EditorStyles.toolbarButton, GUILayout.Width(50)) == true)
				{
					currentState = StateType.Scene;
				}

				if (GUILayout.Toggle(currentState == StateType.Paint, "Paint", EditorStyles.toolbarButton, GUILayout.Width(50)) == true)
				{
					currentState = StateType.Paint;
				}

				if (GUILayout.Toggle(currentState == StateType.Layer, "Layer", EditorStyles.toolbarButton, GUILayout.Width(50)) == true)
				{
					currentState = StateType.Layer;
				}

				if (GUILayout.Toggle(currentState == StateType.Config, "Config", EditorStyles.toolbarButton, GUILayout.Width(50)) == true)
				{
					currentState = StateType.Config;
				}

				EditorGUILayout.Separator();

				EditorGUI.BeginDisabledGroup(P3dHelper.IsAsset(scene) == false);
					if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(40)) == true)
					{
						HandleSave();
					}
				EditorGUI.EndDisabledGroup();

				P3dHelper.BeginColor(Color.green, P3dHelper.IsAsset(scene) == false);
					if (GUILayout.Button("Save As", EditorStyles.toolbarButton, GUILayout.Width(55)) == true)
					{
						HandleSaveAs();
					}
				P3dHelper.EndColor();
			EditorGUILayout.EndHorizontal();

			if (Event.current.type == EventType.MouseUp)
			{
				dragId     = -1;
				dragHandle = -1;
			}

			switch (currentState)
			{
				case StateType.Scene:
				{
					GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
						objectsScrollPosition = GUILayout.BeginScrollView(objectsScrollPosition, GUILayout.Width(200));
							UpdateObjectsPanel();
						GUILayout.EndScrollView();

						DrawViewport();

						materialsScrollPosition = GUILayout.BeginScrollView(materialsScrollPosition, GUILayout.Width(200));
							UpdateMaterialsPanel();
						GUILayout.EndScrollView();
					GUILayout.EndHorizontal();
				}
				break;

				case StateType.Paint:
				{
					GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
						UpdatePaintPanel(200);

						DrawViewport();

						UpdateDynamicsPanel(200);
					GUILayout.EndHorizontal();
				}
				break;

				case StateType.Layer:
				{
					GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
						repeatersScrollPosition = GUILayout.BeginScrollView(repeatersScrollPosition, GUILayout.Width(200));
							UpdateClonersPanel();
						GUILayout.EndScrollView();

						DrawViewport();

						layersScrollPosition = GUILayout.BeginScrollView(layersScrollPosition, GUILayout.Width(200));
							UpdateLayersPanel();
						GUILayout.EndScrollView();
					GUILayout.EndHorizontal();
				}
				break;

				case StateType.Config:
				{
					GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
						sceneScrollPosition = GUILayout.BeginScrollView(sceneScrollPosition, GUILayout.Width(200));
							UpdateScenePanel();
						GUILayout.EndScrollView();

						DrawViewport();

						settingsScrollPosition = GUILayout.BeginScrollView(settingsScrollPosition, GUILayout.Width(200));
							UpdateSettingsPanel();
						GUILayout.EndScrollView();
					GUILayout.EndHorizontal();
				}
				break;
			}

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				EditorGUILayout.LabelField("Undo state " + stateIndex + " of " + states.Count, GUILayout.Width(135));

				EditorGUILayout.Separator();

				scene = (P3dScene)EditorGUILayout.ObjectField(scene, typeof(P3dScene), false, GUILayout.MinWidth(50));

				EditorGUI.BeginDisabledGroup(P3dHelper.IsAsset(scene) == false);
					if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(55)) == true)
					{
						HandleExport();
					}
				EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			HandleDraw();

			if (pendingUpdate == true && Event.current.type == EventType.Repaint)
			{
				pendingUpdate = false;

				UpdatePaintedMats();
			}

			Repaint();
		}

		[SerializeField] private Quaternion cameraOrientation = Quaternion.identity;

		[SerializeField] private bool cameraOrientationSet = false;

		private void DrawViewport()
		{
			var newFullRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)); EditorGUILayout.EndVertical();

			if (newFullRect.size != Vector2.zero)
			{
				fullRect = newFullRect;
			}

			if (Event.current.type != EventType.Layout)
			{
				UpdateCamera();
			}

			if (fullRect.width > 0 && fullRect.height > 0)
			{
				var lights = previewUtil.lights;
				var tilt   = Quaternion.identity;

				if (settings.CamLock == true)
				{
					if (cameraOrientationSet == false)
					{
						cameraOrientation    = previewUtil.camera.transform.rotation;
						cameraOrientationSet = true;
					}

					tilt *= Quaternion.Inverse(cameraOrientation) * previewUtil.camera.transform.rotation;
				}
				else
				{
					cameraOrientationSet = false;
				}

				lights[0].transform.rotation = tilt * Quaternion.Euler(settings.Light0.Pitch, settings.Light0.Yaw, 0.0f);
				lights[0].intensity = settings.Light0.Intensity;
				lights[0].color     = settings.Light0.Color;

				lights[1].transform.rotation = tilt * Quaternion.Euler(settings.Light1.Pitch, settings.Light1.Yaw, 0.0f);
				lights[1].intensity = settings.Light1.Intensity;
				lights[1].color     = settings.Light1.Color;

				BeginPreview(previewUtil, fullRect);

				foreach (var obj in scene.Objs)
				{
					if (obj.Mesh != null)
					{
						for (var i = 0; i < obj.MatIds.Count; i++)
						{
							var mat     = scene.GetMat(obj.MatIds[i]);
							var subMesh = Mathf.Min(i, obj.Mesh.subMeshCount - 1);

							if (mat != null)
							{
								if (mat.Properties == null)
								{
									mat.Properties = new MaterialPropertyBlock();
								}

								foreach (var slot in mat.Slots)
								{
									if (slot.Texture != null)
									{
										mat.Properties.SetTexture(slot.Name, slot.Texture);
									}
								}

								previewUtil.DrawMesh(obj.Mesh, obj.Matrix, mat.Material, subMesh, mat.Properties, null, false);
							}
							else
							{
								previewUtil.DrawMesh(obj.Mesh, obj.Matrix, default(Material), subMesh, null, null, false);
							}
						}
					}
				}

				EndPreview(previewUtil, fullRect);
			}
		}

		private bool ClearPath(Vector2 screenPosition)
		{
			if (dragId >= 0 || dragHandle >= 0)
			{
				return false;
			}

			if (fullRect.Contains(screenPosition) == false)
			{
				return false;
			}

			return true;
		}

		private Ray GetRay()
		{
			var u = (Event.current.mousePosition.x - fullRect.center.x) / fullRect.width;
			var v = (Event.current.mousePosition.y - fullRect.center.y) / fullRect.height;
			var c = previewUtil.camera;
			var k = Quaternion.Euler(v * c.fieldOfView, u * c.fieldOfView, 0.0f) * Vector3.forward;

			if (fullRect.width > fullRect.height)
			{
				k.x *= fullRect.width / fullRect.height;
			}
			else
			{
				k.y *= fullRect.height / fullRect.width;
			}

			return new Ray(c.transform.position, c.transform.TransformVector(k));
		}

		private bool Raycast(Ray ray, ref RaycastHit hit, ref Mesh hitMesh, ref Matrix4x4 hitMatrix)
		{
			hit.distance = float.PositiveInfinity;

			if (scene != null)
			{
				foreach (var entity in scene.Objs)
				{
					var tempHit = default(RaycastHit);

					if (P3dWindowIntersect.IntersectRayMesh(ray, entity.Mesh, entity.Matrix, out tempHit) == true)
					{
						if (tempHit.distance < hit.distance)
						{
							hit       = tempHit;
							hitMesh   = entity.Mesh;
							hitMatrix = entity.Matrix;
						}
					}
				}
			}

			return hit.distance < float.PositiveInfinity;
		}

		private static float LogSlider(string title, float value, float logMin, float logMax)
		{
			var rect   = P3dHelper.Reserve();
			var rectA  = rect; rectA.width = EditorGUIUtility.labelWidth + 50;
			var rectB  = rect; rectB.xMin += EditorGUIUtility.labelWidth + 52;
			var logOld = Mathf.Log10(value);
			var logNew = GUI.HorizontalSlider(rectB, logOld, logMin, logMax);

			if (logOld != logNew)
			{
				value = Mathf.Pow(10.0f, logNew);
			}

			return EditorGUI.FloatField(rectA, title, value);
		}

		private static float Slider(string title, float value, float min, float max)
		{
			var rect  = P3dHelper.Reserve();
			var rectA = rect; rectA.width = EditorGUIUtility.labelWidth + 50;
			var rectB = rect; rectB.xMin += EditorGUIUtility.labelWidth + 52;

			value = GUI.HorizontalSlider(rectB, value, min, max);

			return EditorGUI.FloatField(rectA, title, value);
		}

		private float ScaleViewportValue(float v, float s)
		{
			return s <= 0.0f ? 0.0f : (v / s);
		}

		private void UpdateCamera()
		{
			if (fullRect.Contains(Event.current.mousePosition) == true)
			{
				// Zoom
				if (Event.current.isScrollWheel == true)
				{
					if (ClearPath(Event.current.mousePosition) == true)
					{
						cameraDistance *= Mathf.Clamp(1.0f + Mathf.Sign(Event.current.delta.y) * settings.ZoomSensitivity, 0.01f, 10.0f);
					}
				}
				// Orbit
				else if (Event.current.button == 1)
				{
					var delta = Event.current.delta * settings.TurnSensitivity;

					cameraYaw   += delta.x;
					cameraPitch += delta.y;

					cameraPitch = Mathf.Clamp(cameraPitch, -89.0f, 89.0f);
				}
				// Pan
				else if (Event.current.button == 2)
				{
					var delta = Event.current.delta * cameraDistance / Screen.height;

					cameraOrigin -= previewUtil.camera.transform.right * delta.x;
					cameraOrigin += previewUtil.camera.transform.up    * delta.y;
				}

				// Snap
				if (Event.current.clickCount == 2 && Event.current.button > 0)
				{
					var ray       = GetRay();
					var hit       = default(RaycastHit);
					var hitMesh   = default(Mesh);
					var hitMatrix = default(Matrix4x4);

					if (Raycast(ray, ref hit, ref hitMesh, ref hitMatrix) == true)
					{
						cameraOrigin = hit.point;
					}
				}

				previewUtil.camera.transform.position = cameraOrigin;
				previewUtil.camera.transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0.0f);
				previewUtil.camera.transform.Translate(0.0f, 0.0f, -cameraDistance);
			}
		}

		private int GetMegabytes(List<P3dScene.Layer> layers)
		{
			var bytes = 0L;

			foreach (var layer in layers)
			{
				foreach (var image in layer.Images)
				{
					if (image.Current != null)
					{
						bytes += image.Current.width * image.Current.height * 4;
					}
				}
			}

			return (int)(bytes / 1000000L);
		}
	}
}
#endif