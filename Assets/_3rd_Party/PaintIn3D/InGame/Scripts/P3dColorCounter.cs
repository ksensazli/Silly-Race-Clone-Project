using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component will search the specified paintable texture for pixel colors matching an active and enabled P3dColor.</summary>
	[ExecuteInEditMode]
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dColorCounter")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Color Counter")]
	public class P3dColorCounter : P3dPaintableTextureMonitorMask
	{
		public class Contribution
		{
			public P3dColor Color;
			public int      Count;
			public float    Ratio;
			public byte     R;
			public byte     G;
			public byte     B;
			public byte     A;

			public static Stack<Contribution> Pool = new Stack<Contribution>();
		}

		/// <summary>This stores all active and enabled instances.</summary>
		public static LinkedList<P3dColorCounter> Instances = new LinkedList<P3dColorCounter>(); private LinkedListNode<P3dColorCounter> node;

		/// <summary>Counting all the pixels of a texture can be slow, so you can pick how many times the texture is downsampled before it gets counted. One downsample = half width & height or 1/4 of the pixels. NOTE: The pixel totals will be multiplied to account for this downsampling.</summary>
		public int DownsampleSteps { set { downsampleSteps = value; } get { return downsampleSteps; } } [SerializeField] private int downsampleSteps = 3;

		/// <summary>The RGBA values must be within this range of a color for it to be counted.</summary>
		public float Threshold { set { threshold = value; } get { return threshold; } } [Range(0.0f, 1.0f)] [SerializeField] private float threshold = 0.5f;

		/// <summary>Each color contribution will be stored in this list.</summary>
		public List<Contribution> Contributions { get { return contributions; } } [System.NonSerialized] private List<Contribution> contributions = new List<Contribution>();

		protected override void OnEnable()
		{
			base.OnEnable();

			node = Instances.AddLast(this);
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			Instances.Remove(node); node = null;

			Contribute(0);
		}

		protected override void UpdateMonitor(P3dPaintableTexture paintableTexture, bool preview)
		{
			if (preview == false && paintableTexture.Activated == true)
			{
				var renderTexture = paintableTexture.Current;
				var temporary     = default(RenderTexture);

				if (P3dHelper.Downsample(renderTexture, downsampleSteps, ref temporary) == true)
				{
					Calculate(temporary, temporary, 1 << downsampleSteps);

					P3dHelper.ReleaseRenderTexture(temporary);
				}
				else
				{
					Calculate(renderTexture, temporary, 1);
				}
			}
		}

		private void Calculate(RenderTexture renderTexture, RenderTexture temporary, int scale)
		{
			var threshold32 = (int)(threshold * 255.0f);
			var width       = renderTexture.width;
			var height      = renderTexture.height;
			var texture2D   = P3dHelper.GetReadableCopy(renderTexture);
			var pixels32    = texture2D.GetPixels32();

			P3dHelper.Destroy(texture2D);

			UpdateTotal(renderTexture, width, height, renderTexture.format, scale);

			PrepareContributions();

			for (var y = 0; y < height; y++)
			{
				var offset = y * width;

				for (var x = 0; x < height; x++)
				{
					var index = offset + x;

					if (baked == true && bakedPixels[index] == false)
					{
						continue;
					}

					var pixel32      = pixels32[index];
					var bestIndex    = -1;
					var bestDistance = threshold32;
					
					for (var i = 0; i < P3dColor.InstanceCount; i++)
					{
						var tempColor = contributions[i];
						var distance  = 0;

						distance += System.Math.Abs(tempColor.R - pixel32.r);
						distance += System.Math.Abs(tempColor.G - pixel32.g);
						distance += System.Math.Abs(tempColor.B - pixel32.b);
						distance += System.Math.Abs(tempColor.A - pixel32.a);

						if (distance <= bestDistance)
						{
							bestIndex    = i;
							bestDistance = distance;
						}
					}

					if (bestIndex >= 0)
					{
						contributions[bestIndex].Count++;
					}
				}
			}

			// Multiply totals to account for downsampling
			Contribute(scale);
		}

		private void ClearContributions()
		{
			for (var i = contributions.Count - 1; i >= 0; i--)
			{
				Contribution.Pool.Push(contributions[i]);
			}

			contributions.Clear();
		}

		private void PrepareContributions()
		{
			ClearContributions();

			var color = P3dColor.FirstInstance;

			for (var i = 0; i < P3dColor.InstanceCount; i++)
			{
				var contribution = Contribution.Pool.Count > 0 ? Contribution.Pool.Pop() : new Contribution();
				var color32      = (Color32)color.Color;
				
				contribution.Color = color;
				contribution.Count = 0;
				contribution.R     = color32.r;
				contribution.G     = color32.g;
				contribution.B     = color32.b;
				contribution.A     = color32.a;

				contributions.Add(contribution);

				color = color.NextInstance;
			}
		}

		private void Contribute(int scale)
		{
			var totalRecip = total > 0 ? 1.0f / total : 1.0f;

			for (var i = contributions.Count - 1; i >= 0; i--)
			{
				var contribution = contributions[i];
				
				contribution.Count *= scale;
				contribution.Ratio  = contribution.Count * totalRecip;

				if (contribution.Color != null)
				{
					contribution.Color.Contribute(this, contribution.Count);
				}

				if (contribution.Count <= 0)
				{
					Contribution.Pool.Push(contribution);

					contributions.RemoveAt(i);
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CustomEditor(typeof(P3dColorCounter))]
	public class P3dColorCounter_Editor : P3dPaintableTextureMonitorMask_Editor<P3dColorCounter>
	{
		protected override void OnInspector()
		{
			base.OnInspector();

			BeginError(Any(t => t.DownsampleSteps < 0));
				Draw("downsampleSteps", "Counting all the pixels of a texture can be slow, so you can pick how many times the texture is downsampled before it gets counted. One downsample = half width & height or 1/4 of the pixels. NOTE: The pixel totals will be multiplied to account for this downsampling.");
			EndError();
			Draw("threshold", "The RGBA values must be within this range of a color for it to be counted.");

			Separator();

			BeginDisabled();
				EditorGUILayout.IntField("Total", Target.Total);

				for (var i = 0; i < Target.Contributions.Count; i++)
				{
					var contribution = Target.Contributions[i];
					var rect         = P3dHelper.Reserve();
					var rectL        = rect; rectL.xMax -= (rect.width - EditorGUIUtility.labelWidth) / 2 + 1;
					var rectR        = rect; rectR.xMin = rectL.xMax + 2;

					EditorGUI.IntField(rectL, contribution.Color != null ? contribution.Color.name : "", contribution.Count);
					EditorGUI.ProgressBar(rectR, contribution.Ratio, "Ratio");
				}
			EndDisabled();
		}
	}
}
#endif