using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	public class P3dScene : ScriptableObject
	{
		[System.Serializable]
		public class Image
		{
			public P3dGroup  Group;
			public int       MatId;
			public int       Width;
			public int       Height;
			public byte[]    Pixels;

			public RenderTexture Current;
			public RenderTexture Preview;

			public Image Clone
			{
				get
				{
					var clone = new Image() { Group = Group, MatId = MatId, Width = Width, Height = Height, Pixels = null, Preview = null };

					if (Current != null)
					{
						clone.Current = P3dHelper.GetRenderTexture(Current.descriptor);

						P3dCommandReplace.Blit(clone.Current, Current, Color.white);
					}

					return clone;
				}
			}

			public void Save()
			{
				var copy = P3dHelper.GetReadableCopy(Current);

				if (copy != null)
				{
					Pixels = copy.EncodeToPNG();
					Width  = Current.width;
					Height = Current.height;

					DestroyImmediate(copy);
				}
			}
		}

		[System.Serializable]
		public class Layer
		{
			public string      Name;
			public float       Opacity = 1.0f;
			public List<Image> Images = new List<Image>();

			public Layer Clone
			{
				get
				{
					var clone = new Layer() { Name = Name, Opacity = Opacity };

					foreach (var image in Images)
					{
						clone.Images.Add(image.Clone);
					}

					return clone;
				}
			}

			public Image GetImage(int matId, P3dGroup group)
			{
				foreach (var image in Images)
				{
					if (image.MatId == matId && image.Group == group)
					{
						return image;
					}
				}

				var newImage = new Image();

				newImage.MatId = matId;
				newImage.Group = group;

				Images.Add(newImage);

				return newImage;
			}

			public void Save()
			{
				foreach (var image in Images)
				{
					image.Save();
				}
			}
		}

		[System.Serializable]
		public class Slot
		{
			public string        Name;
			public bool          Dirty;
			public RenderTexture Texture;
		}

		[System.Serializable]
		public class Mat : P3dShaderTemplate.IHasTemplate
		{
			public int         Id;
			public string      Name;
			public Material    Material;
			public P3dShaderTemplate Template;
			public int         Width;
			public int         Height;

			public List<Slot> Slots = new List<Slot>();

			public Dictionary<P3dGroup, MergedLayer> MergedLayers = new Dictionary<P3dGroup, MergedLayer>();

			public Mat Clone
			{
				get
				{
					return new Mat() { Id = Id, Name = Name, Material = Material, Template = Template, Width = Width, Height = Height };
				}
			}

			public bool SizesMatch
			{
				get
				{
					foreach (var slot in Slots)
					{
						if (slot.Texture != null)
						{
							if (slot.Texture.width != Width || slot.Texture.height != Height)
							{
								return false;
							}
						}
					}

					return true;
				}
			}

			public void Resize()
			{
				foreach (var slot in Slots)
				{
					if (slot.Texture != null)
					{
						if (slot.Texture.width != Width || slot.Texture.height != Height)
						{
							var desc           = slot.Texture.descriptor; desc.width = Width; desc.height = Height;
							var resizedTexture = P3dHelper.GetRenderTexture(desc);

							P3dCommandReplace.Blit(resizedTexture, slot.Texture, Color.white);

							P3dHelper.ReleaseRenderTexture(slot.Texture);

							slot.Texture = resizedTexture;
						}
					}
				}
			}

			public void SetTemplate(P3dShaderTemplate template)
			{
				Template = template;
			}

			public P3dShaderTemplate GetTemplate()
			{
				return Template;
			}

			public Slot GetSlot(string name)
			{
				foreach (var slot in Slots)
				{
					if (slot.Name == name)
					{
						slot.Dirty = false;

						return slot;
					}
				}

				var newSlot = new Slot();

				newSlot.Name = name;

				Slots.Add(newSlot);

				return newSlot;
			}

			public Texture GetFinalTexture(P3dGroup group)
			{
				var mergedLayer = default(MergedLayer);

				if (MergedLayers.TryGetValue(group, out mergedLayer) == true)
				{
					return mergedLayer.Final;
				}

				return null;
			}

			[System.NonSerialized]
			public MaterialPropertyBlock Properties;

			public RenderTextureDescriptor Desc
			{
				get
				{
					var desc = new RenderTextureDescriptor(Width, Height, RenderTextureFormat.ARGB32, 0);

					desc.autoGenerateMips = false;
					desc.useMipMap = false;

					return desc;
				}
			}

			public void UpdateMergedLayers(P3dGroup group)
			{
				var mergedLayer = default(MergedLayer);

				if (MergedLayers.TryGetValue(group, out mergedLayer) == false)
				{
					mergedLayer = new MergedLayer();

					MergedLayers.Add(group, mergedLayer);
				}

				mergedLayer.Dirty = false;
			}
		}

		[System.Serializable]
		public class Obj
		{
			public string     Name;
			public Mesh       Mesh;
			public bool       Paintable = true;
			public P3dCoord   Coord;
			public Transform  Transform;
			public Vector3    Position;
			public Quaternion Rotation = Quaternion.identity;
			public Vector3    Scale    = Vector3.one;

			public List<int> MatIds = new List<int>();

			public Matrix4x4 Matrix
			{
				get
				{
					return Matrix4x4.TRS(Position, Rotation, Scale);
				}
			}

			public Obj Clone
			{
				get
				{
					return new Obj() { Name = Name, Mesh = Mesh, Position = Position, Rotation = Rotation, Scale = Scale, Transform = Transform, Paintable = Paintable, Coord = Coord, MatIds = new List<int>(MatIds) };
				}
			}
		}

		[System.Serializable]
		public class Cloner : IClone
		{
			public string  Name;
			public Vector3 Position;
			public Vector3 Euler;
			public bool    Flip;

			public void Transform(ref Matrix4x4 posMatrix, ref Matrix4x4 rotMatrix)
			{
				var p   = Position;
				var r   = Quaternion.Euler(Euler);
				var s   = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f));
				var tp  = Matrix4x4.Translate(p);
				var rp  = Matrix4x4.Rotate(r);
				var ti  = Matrix4x4.Translate(-p);
				var ri  = Matrix4x4.Rotate(Quaternion.Inverse(r));
				var mat = tp * rp * s * ri * ti;

				if (Flip == true)
				{
					posMatrix = mat * posMatrix;
				}
				else
				{
					posMatrix = mat * posMatrix;
					rotMatrix = mat * rotMatrix;
				}
			}
		}

		public class MergedLayer
		{
			public RenderTexture Under;
			public RenderTexture Above;
			public RenderTexture Final;
			public Layer         Layer;
			public bool          Dirty;

			public void Clear()
			{
				Under = P3dHelper.ReleaseRenderTexture(Under);
				Above = P3dHelper.ReleaseRenderTexture(Above);
				Final = P3dHelper.ReleaseRenderTexture(Final);
			}
		}

		public List<Layer> Layers = new List<Layer>();

		public List<Mat> Mats = new List<Mat>();

		public List<Obj> Objs = new List<Obj>();

		public List<Cloner> Cloners = new List<Cloner>();

		public void Clear()
		{
			foreach (var mat in Mats)
			{
				foreach (var mergedLayer in mat.MergedLayers)
				{
					mergedLayer.Value.Clear();
				}

				mat.MergedLayers.Clear();
			}

			foreach (var layer in Layers)
			{
				foreach (var image in layer.Images)
				{
					image.Pixels = null;
				}
			}
		}

		public void UpdateMergedLayers(Layer currentLayer)
		{
			foreach (var mat in Mats)
			{
				foreach (var mergedLayer in mat.MergedLayers.Values)
				{
					mergedLayer.Dirty = true;

					if (mergedLayer.Layer != currentLayer)
					{
						mergedLayer.Clear();

						mergedLayer.Layer = currentLayer;
					}
				}

				if (mat.Template != null)
				{
					foreach (var slot in mat.Template.Slots)
					{
						mat.UpdateMergedLayers(slot.WriteR.SourceGroup);
						mat.UpdateMergedLayers(slot.WriteG.SourceGroup);
						mat.UpdateMergedLayers(slot.WriteB.SourceGroup);
						mat.UpdateMergedLayers(slot.WriteA.SourceGroup);
					}
				}

				foreach (var pair in mat.MergedLayers)
				{
					var group       = pair.Key;
					var mergedLayer = pair.Value;

					if (mergedLayer.Dirty == true)
					{
						mergedLayer.Clear();
					}
					else
					{
						var currentLayerIndex = Layers.IndexOf(currentLayer);

						if (mergedLayer.Under == null)
						{
							mergedLayer.Under = P3dHelper.GetRenderTexture(mat.Desc);

							var groupData = P3dGroupData.GetGroupData(group);

							if (groupData != null)
							{
								P3dCommandReplace.Blit(mergedLayer.Under, groupData.DefaultTexture, groupData.DefaultColor);
							}
							else
							{
								P3dCommandReplace.Blit(mergedLayer.Under, default(Texture), default(Color));
							}

							for (var i = 0; i < currentLayerIndex; i++)
							{
								TryBlendInto(ref mergedLayer.Under, Layers[i], mat.Id, group);
							}
						}

						// Last layer?
						if (currentLayerIndex == Layers.Count - 1)
						{
							if (mergedLayer.Above != null)
							{
								mergedLayer.Above = P3dHelper.ReleaseRenderTexture(mergedLayer.Above);
							}
						}
						else
						{
							if (mergedLayer.Above == null)
							{
								mergedLayer.Above = P3dHelper.GetRenderTexture(mat.Desc);

								P3dCommandReplace.Blit(mergedLayer.Above, default(Texture), default(Color));

								for (var i = currentLayerIndex + 1; i < Layers.Count; i++)
								{
									TryBlendInto(ref mergedLayer.Above, Layers[i], mat.Id, group);
								}
							}
						}

						if (mergedLayer.Final == null)
						{
							mergedLayer.Final = P3dHelper.GetRenderTexture(mat.Desc);
						}

						P3dCommandReplace.Blit(mergedLayer.Final, mergedLayer.Under, Color.white);

						TryBlendInto(ref mergedLayer.Final, currentLayer, mat.Id, group);

						if (mergedLayer.Above != null)
						{
							mergedLayer.Final = P3dCommandFill.Blit(mergedLayer.Final, P3dBlendMode.AlphaBlend, mergedLayer.Above, Color.white, 1.0f, 0.0f);
						}
					}
				}
			}
		}

		private static void TryBlendInto(ref RenderTexture renderTexture, Layer layer, int matId, P3dGroup group)
		{
			var image   = layer.GetImage(matId, group);
			var texture = image.Preview ?? image.Current;

			if (texture != null)
			{
				renderTexture = P3dCommandFill.Blit(renderTexture, P3dBlendMode.AlphaBlend, texture, Color.white, layer.Opacity, 0.0f);
			}
		}

		public void Save()
		{
			foreach (var layer in Layers)
			{
				layer.Save();
			}
		}

		public void RemoveObj(Obj obj)
		{
			if (obj != null)
			{
				Objs.Remove(obj);
			}
		}

		public Mat GetMat(int matId)
		{
			foreach (var mat in Mats)
			{
				if (mat.Id == matId)
				{
					return mat;
				}
			}

			return null;
		}

		public bool MatNameValid(string name, int maxCount = 1)
		{
			if (string.IsNullOrEmpty(name) == true)
			{
				return false;
			}

			var count = 0;

			foreach (var mat in Mats)
			{
				if (mat.Name == name)
				{
					count += 1;

					if (count > maxCount)
					{
						return false;
					}
				}
			}

			return true;
		}

		public void RemoveMat(Mat mat)
		{
			if (mat != null)
			{
				Mats.Remove(mat);
			}
		}

		public bool ObjExists(Transform source)
		{
			foreach (var obj in Objs)
			{
				if (obj.Transform == source)
				{
					return true;
				}
			}

			return false;
		}

		public Mat AddMat(Material material, P3dShaderTemplate template, int size)
		{
			var highestId = 0;
			var mat       = default(Mat);

			for (var i = 0; i < Mats.Count; i++)
			{
				var tempMat = Mats[i];

				if (tempMat.Id > highestId)
				{
					highestId = tempMat.Id;
				}

				if (tempMat.Material == material)
				{
					mat = tempMat;
				}
			}

			if (mat == null)
			{
				mat = new Mat();
				
				mat.Id       = highestId + 1;
				mat.Name     = material != null ? material.name : "New Material";
				mat.Material = material;
				mat.Width    = size;
				mat.Height   = size;

				Mats.Add(mat);
			}

			mat.Template = template;

			return mat;
		}

		public void AddObj(Transform source, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material[] materials, P3dShaderTemplate[] templates, int size)
		{
			var entity = new Obj();

			entity.Name      = source.name;
			entity.Transform = source;
			entity.Mesh      = mesh;
			entity.Position  = position;
			entity.Rotation  = rotation;
			entity.Scale     = scale;

			for (var i = 0; i < materials.Length; i++)
			{
				var mat = AddMat(materials[i], templates[i], size);

				if (mat.Slots.Count == 0 && mat.Template != null)
				{
					foreach (var slot in mat.Template.Slots)
					{
						var copy = new Slot();

						copy.Name = slot.Name;

						mat.Slots.Add(copy);
					}
				}

				entity.MatIds.Add(mat.Id);
			}

			Objs.Add(entity);
		}

		public Cloner AddCloner(string name, Vector3 position, Vector3 euler)
		{
			var cloner = new Cloner();

			cloner.Name     = name;
			cloner.Position = position;
			cloner.Euler    = euler;

			Cloners.Add(cloner);

			return cloner;
		}

		public void RemoveRepeater(Cloner repeater)
		{
			if (repeater != null)
			{
				Cloners.Remove(repeater);
			}
		}

		public Layer AddLayer()
		{
			var layer = new Layer();

			layer.Name = "New Layer";

			Layers.Add(layer);

			return layer;
		}

		public void RemoveLayer(Layer layer)
		{
			if (layer != null)
			{
				Layers.Remove(layer);
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	[CustomEditor(typeof(P3dScene))]
	public class P3dScene_Editor : P3dEditor<P3dScene>
	{
		protected override void OnInspector()
		{
			if (GUILayout.Button("Open") == true)
			{
				P3dPainter.GetWindow().Scene = Target;
			}
		}
	}
}
#endif