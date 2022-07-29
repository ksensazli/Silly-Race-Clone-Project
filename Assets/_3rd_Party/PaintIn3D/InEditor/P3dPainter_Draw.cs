#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PaintIn3D
{
	public partial class P3dPainter
	{
		private static List<P3dCommand> pendingCommands = new List<P3dCommand>();

		private static List<P3dCommand> clonedCommands = new List<P3dCommand>();

		private static List<IClone> tempCloners = new List<IClone>();

		private static HashSet<P3dGroup> paintedGroups = new HashSet<P3dGroup>();

		private bool painting;

		private bool previewDrawn;

		private bool pendingUpdate;

		private void HandleDraw()
		{
			if (ClearPath(Event.current.mousePosition) == true && currentPaintMaterial != null && currentLayer != null)
			{
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					StoreState();

					painting = true;
				}

				var ray       = GetRay();
				var hit       = default(RaycastHit);
				var hitMesh   = default(Mesh);
				var hitMatrix = default(Matrix4x4);

				if (Raycast(ray, ref hit, ref hitMesh, ref hitMatrix) == true)
				{
					var preview = painting == false;

					if (painting == true && settings.PaintMode == ModeType.Point && Event.current.type != EventType.MouseUp)
					{
						preview = true;
					}

					if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove || Event.current.type == EventType.Repaint)
					{
						DoPaint(preview, Event.current.pressure, ray, hit, hitMesh, hitMatrix);
					}
				}
			}

			if (Event.current.type == EventType.MouseUp)
			{
				painting = false;
			}
		}

		private void SubmitLocation(RaycastHit hit, Mesh hitMesh, Matrix4x4 hitMatrix, Vector3 finalPosition)
		{
			if (settings.PaintMode == ModeType.Triangle)
			{
				var triangles = hitMesh.triangles;
				var positions = hitMesh.vertices;
				var offset    = hit.triangleIndex * 3;
				var positionA = hitMatrix.MultiplyPoint(positions[triangles[offset + 0]]);
				var positionB = hitMatrix.MultiplyPoint(positions[triangles[offset + 1]]);
				var positionC = hitMatrix.MultiplyPoint(positions[triangles[offset + 2]]);

				P3dCommandDecal.Instance.SetLocation(positionA, positionB, positionC);
			}
			else
			{
				P3dCommandDecal.Instance.SetLocation(finalPosition);
			}
		}

		private void DoPaint(bool preview, float pressure, Ray ray, RaycastHit hit, Mesh hitMesh, Matrix4x4 hitMatrix)
		{
			pendingCommands.Clear();

			var finalPosition = hit.point;
			var finalAngle    = settings.PaintAngle;
			var finalOpacity  = settings.PaintOpacity;
			var finalRadius   = settings.PaintScale;
			var finalRotation = Quaternion.LookRotation(ray.direction);
			var finalAspect   = P3dHelper.GetAspect(paintBrushData.Shape);

			paintBrushData.Modifiers.ModifyAngle(ref finalAngle, false, pressure);
			paintBrushData.Modifiers.ModifyOpacity(ref finalOpacity, false, pressure);
			paintBrushData.Modifiers.ModifyPosition(ref finalPosition, false, pressure);
			paintBrushData.Modifiers.ModifyRadius(ref finalRadius, false, pressure);

			var finalSize = Vector3.one * finalRadius;

			switch (currentPaintMaterial.Style)
			{
				case P3dPaintMaterial.StyleType.Seamless:
				{
					foreach (var slot in currentPaintMaterial.Slots)
					{
						var tileMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(settings.PaintTile, settings.PaintTile, settings.PaintTile));
						var finalColor = Color.white;

						if (slot.Group == settings.ColorModifies)
						{
							finalColor *= settings.PaintColor;

							paintBrushData.Modifiers.ModifyColor(ref finalColor, false, pressure);
						}

						P3dCommandDecal.Instance.SetState(preview, slot.Group); // Store group in priority

						P3dCommandDecal.Instance.SetShape(finalRotation, P3dHelper.ScaleAspect(finalSize, finalAspect), finalAngle);

						SubmitLocation(hit, hitMesh, hitMatrix, finalPosition);

						P3dCommandDecal.Instance.SetMaterial(P3dBlendMode.AlphaBlend, null, paintBrushData.Shape, paintBrushData.ShapeChannel, 1.0f, 1.0f, settings.PaintNormalBack, settings.PaintNormalFront, settings.PaintNormalFade, finalColor, finalOpacity, slot.Texture, tileMatrix, 1.0f);

						pendingCommands.Add(P3dCommandDecal.Instance.SpawnCopy());
					}
				}
				break;

				case P3dPaintMaterial.StyleType.Decal:
				{
					var shape = paintBrushData.Shape != null ? paintBrushData.Shape : currentPaintMaterial.Shape;

					if (shape != null)
					{
						finalAspect = P3dHelper.GetAspect(shape);
					}
					else
					{
						foreach (var slot in currentPaintMaterial.Slots)
						{
							if (slot.Texture != null)
							{
								finalAspect = P3dHelper.GetAspect(slot.Texture); break;
							}
						}
					}

					foreach (var slot in currentPaintMaterial.Slots)
					{
						var finalColor = Color.white;

						if (slot.Group == settings.ColorModifies)
						{
							finalColor *= settings.PaintColor;

							paintBrushData.Modifiers.ModifyColor(ref finalColor, false, pressure);
						}

						P3dCommandDecal.Instance.SetState(preview, slot.Group); // Store group in priority

						P3dCommandDecal.Instance.SetShape(finalRotation, P3dHelper.ScaleAspect(finalSize, finalAspect), finalAngle);

						SubmitLocation(hit, hitMesh, hitMatrix, finalPosition);

						P3dCommandDecal.Instance.SetMaterial(P3dBlendMode.AlphaBlend, slot.Texture, shape, paintBrushData.ShapeChannel, 1.0f, 1.0f, settings.PaintNormalBack, settings.PaintNormalFront, settings.PaintNormalFade, finalColor, finalOpacity, null, Matrix4x4.identity, 1.0f);

						pendingCommands.Add(P3dCommandDecal.Instance.SpawnCopy());
					}
				}
				break;
			}

			// Clone pending commands
			clonedCommands.Clear();

			tempCloners.Clear();

			currentCloners.RemoveAll(c => scene.Cloners.Contains(c) == false);

			foreach (var cloner in currentCloners)
			{
				tempCloners.Add(cloner);
			}

			// Remove excluded or non-isolated groups (stored in priority)
			if (isolatedGroups.Count > 0)
			{
				pendingCommands.RemoveAll(c => isolatedGroups.Contains(c.Priority) == false);
			}
			else
			{
				pendingCommands.RemoveAll(c => excludedGroups.Contains(c.Priority) == true);
			}

			foreach (var command in pendingCommands)
			{
				clonedCommands.Add(command.SpawnCopy());

				P3dClone.BuildCloners(tempCloners);

				for (var c = 0; c < P3dClone.ClonerCount; c++)
				{
					for (var m = 0; m < P3dClone.MatrixCount; m++)
					{
						var copy = command.SpawnCopy();

						P3dClone.Clone(copy, c, m);

						clonedCommands.Add(copy);
					}
				}
			}

			pendingCommands.Clear();

			// Paint commands
			paintedGroups.Clear();

			foreach (var command in clonedCommands)
			{
				foreach (var mat in scene.Mats)
				{
					var image = currentLayer.GetImage(mat.Id, command.Priority); // Group is stored in priority

					foreach (var obj in scene.Objs)
					{
						if (obj.Mesh != null && obj.Paintable == true)
						{
							for (var j = 0; j < obj.MatIds.Count; j++)
							{
								if (mat.Id == obj.MatIds[j])
								{
									var subMesh = Mathf.Max(j, obj.Mesh.subMeshCount - 1);

									Render(command, mat, image, obj, subMesh);
								}
							}
						}
					}
				}

				command.Pool();
			}

			pendingUpdate = true;
		}

		private void Render(P3dCommand command, P3dScene.Mat mat, P3dScene.Image image, P3dScene.Obj obj, int subMesh)
		{
			var oldActive = RenderTexture.active;

			if (image.Current == null)
			{
				if (image.Width > 0 && image.Height > 0 && image.Pixels != null && image.Pixels.Length > 0)
				{
					var texture = new Texture2D(1, 1);

					if (texture.LoadImage(image.Pixels) == true)
					{
						var desc = mat.Desc;

						desc.width  = image.Width;
						desc.height = image.Height;

						image.Current = P3dHelper.GetRenderTexture(mat.Desc);

						P3dCommandReplace.BlitFast(image.Current, texture, Color.white);
					}
					else
					{
						image.Current = P3dHelper.GetRenderTexture(mat.Desc);

						P3dCommandReplace.BlitFast(image.Current, default(Texture), default(Color));
					}

					DestroyImmediate(texture);
				}
				else
				{
					image.Current = P3dHelper.GetRenderTexture(mat.Desc);

					P3dCommandReplace.BlitFast(image.Current, default(Texture), default(Color));
				}
			}

			var swap = P3dHelper.GetRenderTexture(image.Current.descriptor);

			if (command.Preview == true)
			{
				if (image.Preview == null)
				{
					image.Preview = P3dHelper.GetRenderTexture(image.Current.descriptor);

					P3dCommandReplace.BlitFast(image.Preview, image.Current, Color.white);
				}

				P3dCommandReplace.BlitFast(swap, image.Preview, Color.white);

				command.Apply(image.Preview);
			}
			else
			{
				P3dCommandReplace.BlitFast(swap, image.Current, Color.white);

				command.Apply(image.Current);
			}

			RenderTexture.active = swap;

			if (command.RequireMesh == true)
			{
				P3dHelper.Draw(command.Material, obj.Mesh, obj.Matrix, subMesh, obj.Coord);
			}
			else
			{
				P3dHelper.Draw(command.Material);
			}

			RenderTexture.active = oldActive;

			if (command.Preview == true)
			{
				P3dHelper.ReleaseRenderTexture(image.Preview);

				image.Preview = swap;

				previewDrawn = true;
			}
			else
			{
				P3dHelper.ReleaseRenderTexture(image.Current);

				image.Current = swap;
			}

			paintedGroups.Add(command.Priority); // Group is stored in priority
		}

		private void UpdatePaintedMats()
		{
			// Mark
			foreach (var mat in scene.Mats)
			{
				foreach (var slot in mat.Slots)
				{
					slot.Dirty = true;
				}
			}

			/*
				if (mat.Template != null)
				{
					foreach (var slot in mat.Template.Slots)
					{
						if (paintedGroups.Contains(slot.WriteR.SourceGroup) == true ||
							paintedGroups.Contains(slot.WriteG.SourceGroup) == true ||
							paintedGroups.Contains(slot.WriteB.SourceGroup) == true ||
							paintedGroups.Contains(slot.WriteA.SourceGroup) == true)
						{
							var matSlot = mat.Slots.Find(s => s.Name == slot.Name);

							if (matSlot != null)
							{
								matSlot.Dirty = true;
							}
						}
					}
				}
			}
			*/

			scene.UpdateMergedLayers(currentLayer);

			foreach (var mat in scene.Mats)
			{
				if (mat.Template != null)
				{
					foreach (var templateSlot in mat.Template.Slots)
					{
						var sceneSlot = mat.GetSlot(templateSlot.Name);

						if (sceneSlot.Texture == null)
						{
							sceneSlot.Texture = P3dHelper.GetRenderTexture(mat.Desc);
						}

						var textureR = mat.GetFinalTexture(templateSlot.WriteR.SourceGroup);
						var textureG = mat.GetFinalTexture(templateSlot.WriteG.SourceGroup);
						var textureB = mat.GetFinalTexture(templateSlot.WriteB.SourceGroup);
						var textureA = mat.GetFinalTexture(templateSlot.WriteA.SourceGroup);
						var channelR = P3dHelper.IndexToVector((int)templateSlot.WriteR.SourceChannel);
						var channelG = P3dHelper.IndexToVector((int)templateSlot.WriteG.SourceChannel);
						var channelB = P3dHelper.IndexToVector((int)templateSlot.WriteB.SourceChannel);
						var channelA = P3dHelper.IndexToVector((int)templateSlot.WriteA.SourceChannel);

						P3dCommandReplaceChannels.Blit(sceneSlot.Texture, textureR, textureG, textureB, textureA, channelR, channelG, channelB, channelA);
					}
				}
			}

			// Sweep
			foreach (var mat in scene.Mats)
			{
				for (var i = 0; i < mat.Slots.Count; i++)
				{
					var slot = mat.Slots[i];

					if (slot.Dirty == true)
					{
						P3dHelper.ReleaseRenderTexture(slot.Texture);

						mat.Slots.RemoveAt(i);
					}
				}
			}
		}
	}
}
#endif