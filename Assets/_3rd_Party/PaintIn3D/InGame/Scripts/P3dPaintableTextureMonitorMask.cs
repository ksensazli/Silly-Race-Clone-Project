using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This base class allows you to quickly create components that listen for changes to the specified P3dPaintableTexture.</summary>
	public abstract class P3dPaintableTextureMonitorMask : P3dPaintableTextureMonitor
	{
		/// <summary>If you want this component to accurately count pixels relative to a paintable mesh, then specify the mesh here.
		/// NOTE: For best results this should be the original mesh, NOT the seam-fixed version.</summary>
		public Mesh Mesh { set { mesh = value; } get { return mesh; } } [SerializeField] private Mesh mesh;

		/// <summary>The previously counted total amount of pixels.</summary>
		public int Total { get { return total; } } [SerializeField] protected int total;

		[SerializeField]
		protected bool baked;

		[SerializeField]
		private Mesh bakedMesh;

		[SerializeField]
		private Vector2Int bakedSize;

		[SerializeField]
		protected List<bool> bakedPixels;

		private static Material cachedMaterial;

		protected virtual void ClearBake()
		{
			if (baked == true)
			{
				baked     = false;
				bakedMesh = null;
				bakedSize = Vector2Int.zero;
				bakedPixels.Clear();
			}
		}

		protected void UpdateTotal(RenderTexture temporary, int width, int height, RenderTextureFormat format, int scale)
		{
			if (mesh != null)
			{
				if (baked == false || bakedMesh != mesh || bakedSize.x != width || bakedSize.y != height)
				{
					Bake(temporary, width, height, format, scale);
				}
			}
			else
			{
				ClearBake();

				total = width * height * scale;
			}
		}

		private void Bake(RenderTexture temporary, int width, int height, RenderTextureFormat format, int scale)
		{
			if (bakedPixels == null)
			{
				bakedPixels = new List<bool>();
			}
			else
			{
				bakedPixels.Clear();
			}

			baked     = true;
			bakedMesh = mesh;
			bakedSize = new Vector2Int(width, height);

			if (cachedMaterial == null)
			{
				cachedMaterial = P3dShader.BuildMaterial("Hidden/Paint in 3D/White");
			}

			cachedMaterial.SetVector(P3dShader._Channel, P3dHelper.IndexToVector((int)PaintableTexture.Coord));

			// Write to temp RenderTexture
			var oldActive     = RenderTexture.active;
			var renderTexture = temporary;

			if (temporary == null)
			{
				var desc = new RenderTextureDescriptor(width, height, format, 0);

				renderTexture = P3dHelper.GetRenderTexture(desc);
			}

			RenderTexture.active = renderTexture;

			GL.Clear(true, true, Color.black);

			cachedMaterial.SetPass(0);

			Graphics.DrawMeshNow(mesh, Matrix4x4.identity, PaintableTexture.Slot.Index);

			RenderTexture.active = oldActive;

			// Get readable copy
			var readable = P3dHelper.GetReadableCopy(renderTexture);

			if (temporary == null)
			{
				P3dHelper.ReleaseRenderTexture(renderTexture);
			}

			// Run through pixels to count total and build binary mask
			bakedPixels.Capacity = width * height;

			total = 0;

			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					if (readable.GetPixel(x, y).r > 0.5f)
					{
						bakedPixels.Add(true);

						total += scale;
					}
					else
					{
						bakedPixels.Add(false);
					}
				}
			}

			// Clean up
			P3dHelper.Destroy(readable);
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	public class P3dPaintableTextureMonitorMask_Editor<T> : P3dPaintableTextureMonitor_Editor<T>
		where T : P3dPaintableTextureMonitor
	{
		protected override void OnInspector()
		{
			base.OnInspector();

			Draw("mesh", "If you want this component to accurately count pixels relative to a paintable mesh, then specify the mesh here.\n\nNOTE: For best results this should be the original mesh, NOT the seam-fixed version.");
		}
	}
}
#endif