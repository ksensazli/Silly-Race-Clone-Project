using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component will total up all RGBA channels in the specified P3dPaintableTexture that exceed the threshold value.</summary>
	[ExecuteInEditMode]
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dChannelCounter")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Channel Counter")]
	public class P3dChannelCounter : P3dPaintableTextureMonitorMask
	{
		/// <summary>This stores all active and enabled instances.</summary>
		public static LinkedList<P3dChannelCounter> Instances = new LinkedList<P3dChannelCounter>(); private LinkedListNode<P3dChannelCounter> node;

		/// <summary>Counting all the pixels of a texture can be slow, so you can pick how many times the texture is downsampled before it gets counted. One downsample = half width & height or 1/4 of the pixels.
		/// NOTE: The pixel totals will be multiplied to account for this downsampling.</summary>
		public int DownsampleSteps { set { downsampleSteps = value; } get { return downsampleSteps; } } [SerializeField] private int downsampleSteps = 3;

		/// <summary>The RGBA value must be higher than this for it to be counted.</summary>
		public float Threshold { set { threshold = value; } get { return threshold; } } [Range(0.0f, 1.0f)] [SerializeField] private float threshold = 0.5f;

		/// <summary>The previously counted amount of pixels with a red channel value above the threshold.</summary>
		public int SolidR { get { return solidR; } } [SerializeField] private int solidR;

		/// <summary>The previously counted amount of pixels with a green channel value above the threshold.</summary>
		public int SolidG { get { return solidG; } } [SerializeField] private int solidG;

		/// <summary>The previously counted amount of pixels with a blue channel value above the threshold.</summary>
		public int SolidB { get { return solidB; } } [SerializeField] private int solidB;

		/// <summary>The previously counted amount of pixels with a alpha channel value above the threshold.</summary>
		public int SolidA { get { return solidA; } } [SerializeField] private int solidA;

		/// <summary>The SolidR/Total value, allowing you to easily see how much % of the red channel is above the threshold.</summary>
		public float RatioR { get { return total > 0 ? solidR / (float)total : 0.0f; } }

		/// <summary>The SolidG/Total value, allowing you to easily see how much % of the green channel is above the threshold.</summary>
		public float RatioG { get { return total > 0 ? solidG / (float)total : 0.0f; } }

		/// <summary>The SolidB/Total value, allowing you to easily see how much % of the blue channel is above the threshold.</summary>
		public float RatioB { get { return total > 0 ? solidB / (float)total : 0.0f; } }

		/// <summary>The SolidA/Total value, allowing you to easily see how much % of the alpha channel is above the threshold.</summary>
		public float RatioA { get { return total > 0 ? solidA / (float)total : 0.0f; } }

		/// <summary>The RatioR/G/B/A values packed into a Vector4.</summary>
		public Vector4 RatioRGBA
		{
			get
			{
				if (total > 0)
				{
					var ratios = default(Vector4);
					var scale  = 1.0f / total;

					ratios.x = Mathf.Clamp01(solidR * scale);
					ratios.y = Mathf.Clamp01(solidG * scale);
					ratios.z = Mathf.Clamp01(solidB * scale);
					ratios.w = Mathf.Clamp01(solidA * scale);

					return ratios;
				}

				return Vector4.zero;
			}
		}

		/// <summary>The sum of all Total values.</summary>
		public static long GetTotal(ICollection<P3dChannelCounter> counters = null)
		{
			var total = 0L; foreach (var counter in counters ?? Instances) { total += counter.total; } return total;
		}

		/// <summary>The sum of all SolidR values.</summary>
		public static long GetSolidR(ICollection<P3dChannelCounter> counters = null)
		{
			var solid = 0L; foreach (var counter in counters ?? Instances) { solid += counter.solidR; } return solid;
		}

		/// <summary>The sum of all SolidG values.</summary>
		public static long GetSolidG(ICollection<P3dChannelCounter> counters = null)
		{
			var solid = 0L; foreach (var counter in counters ?? Instances) { solid += counter.solidG; } return solid;
		}

		/// <summary>The sum of all SolidB values.</summary>
		public static long GetSolidB(ICollection<P3dChannelCounter> counters = null)
		{
			var solid = 0L; foreach (var counter in counters ?? Instances) { solid += counter.solidB; } return solid;
		}

		/// <summary>The sum of all SolidA values.</summary>
		public static long GetSolidA(ICollection<P3dChannelCounter> counters = null)
		{
			var solid = 0L; foreach (var counter in counters ?? Instances) { solid += counter.solidA; } return solid;
		}

		/// <summary>The average of all RatioRGBA values.</summary>
		public static Vector4 GetRatioRGBA(ICollection<P3dChannelCounter> counters = null)
		{
			if (counters == null) counters = Instances;

			if (counters.Count > 0)
			{
				var total = Vector4.zero;

				foreach (var counter in counters)
				{
					total += counter.RatioRGBA;
				}

				return total / Instances.Count;
			}

			return Vector4.zero;
		}

		protected override void OnEnable()
		{
			node = Instances.AddLast(this);

			base.OnEnable();
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			Instances.Remove(node); node = null;
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
			var threshold32 = (byte)(threshold * 255.0f);
			var width       = renderTexture.width;
			var height      = renderTexture.height;
			var texture2D   = P3dHelper.GetReadableCopy(renderTexture);
			var pixels32    = texture2D.GetPixels32();

			P3dHelper.Destroy(texture2D);

			UpdateTotal(temporary, width, height, renderTexture.format, scale);

			// Reset totals
			solidR = 0;
			solidG = 0;
			solidB = 0;
			solidA = 0;

			// Calculate totals
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

					var pixel32 = pixels32[index];

					if (pixel32.r >= threshold32) solidR++;
					if (pixel32.g >= threshold32) solidG++;
					if (pixel32.b >= threshold32) solidB++;
					if (pixel32.a >= threshold32) solidA++;
				}
			}

			// Scale totals to account for downsampling
			solidR *= scale;
			solidG *= scale;
			solidB *= scale;
			solidA *= scale;
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CustomEditor(typeof(P3dChannelCounter))]
	public class P3dChannelCounter_Editor : P3dPaintableTextureMonitorMask_Editor<P3dChannelCounter>
	{
		protected override void OnInspector()
		{
			base.OnInspector();

			BeginError(Any(t => t.DownsampleSteps < 0));
				Draw("downsampleSteps", "Counting all the pixels of a texture can be slow, so you can pick how many times the texture is downsampled before it gets counted. One downsample = half width & height or 1/4 of the pixels. NOTE: The pixel totals will be multiplied to account for this downsampling.");
			EndError();
			Draw("threshold", "The RGBA value must be higher than this for it to be counted.");

			Separator();

			BeginDisabled();
				EditorGUILayout.IntField("Total", Target.Total);

				DrawChannel("solidR", "Ratio R", Target.RatioR);
				DrawChannel("solidG", "Ratio G", Target.RatioG);
				DrawChannel("solidB", "Ratio B", Target.RatioB);
				DrawChannel("solidA", "Ratio A", Target.RatioA);
			EndDisabled();
		}

		private void DrawChannel(string solidTitle, string ratioTitle, float ratio)
		{
			var rect  = P3dHelper.Reserve();
			var rectL = rect; rectL.xMax -= (rect.width - EditorGUIUtility.labelWidth) / 2 + 1;
			var rectR = rect; rectR.xMin = rectL.xMax + 2;

			EditorGUI.PropertyField(rectL, serializedObject.FindProperty(solidTitle));
			EditorGUI.ProgressBar(rectR, ratio, ratioTitle);
		}
	}
}
#endif