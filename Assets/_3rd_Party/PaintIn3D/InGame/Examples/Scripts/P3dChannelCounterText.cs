using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component allows you to output the totals of all the specified pixel counters to a UI Text component.</summary>
	[RequireComponent(typeof(Text))]
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dChannelCounterText")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Channel Counter Text")]
	public class P3dChannelCounterText : MonoBehaviour
	{
		public enum ChannelType
		{
			Red,
			Green,
			Blue,
			Alpha,
			InverseAlpha
		}

		public enum OutputType
		{
			Percentage,
			Pixels
		}

		/// <summary>This allows you to specify the counters that will be used.
		/// Zero = All and and enabled counters in the scene.</summary>
		public List<P3dChannelCounter> Counters { get { if (counters == null) counters = new List<P3dChannelCounter>(); return counters; } } [SerializeField] private List<P3dChannelCounter> counters;

		/// <summary>This allows you to choose which channel will be output to the UI Text.</summary>
		public ChannelType Channel { set { channel = value; } get { return channel; } } [SerializeField] private ChannelType channel;

		/// <summary>This allows you to choose which value will be output to the UI Text.</summary>
		public OutputType Output { set { output = value; } get { return output; } } [SerializeField] private OutputType output;

		/// <summary>This allows you to round the output value when using <b>Format = Percentage</b>.
		/// 1 = One decimal place.</summary>
		public int Round { set { round = value; } get { return round; } } [SerializeField] private int round = 1;

		/// <summary>This allows you to set the format of the text, where the {0} token will contain the number.</summary>
		public string Format { set { format = value; } get { return format; } } [Multiline] [SerializeField] private string format = "{0}";

		[System.NonSerialized]
		private Text cachedText;

		protected virtual void OnEnable()
		{
			cachedText = GetComponent<Text>();
		}

		protected virtual void Update()
		{
			var finalCounters = counters.Count > 0 ? counters : null;

			switch (output)
			{
				case OutputType.Percentage:
				{
					var ratios = P3dChannelCounter.GetRatioRGBA(finalCounters);

					switch (channel)
					{
						case ChannelType.Red:   OutputRatio(ratios.x); break;
						case ChannelType.Green: OutputRatio(ratios.y); break;
						case ChannelType.Blue:  OutputRatio(ratios.z); break;
						case ChannelType.Alpha: OutputRatio(ratios.w); break;

						case ChannelType.InverseAlpha: OutputRatio(1.0f - ratios.w); break;
					}
				}
				break;

				case OutputType.Pixels:
				{
					switch (channel)
					{
						case ChannelType.Red:   OutputSolid(P3dChannelCounter.GetSolidR(finalCounters)); break;
						case ChannelType.Green: OutputSolid(P3dChannelCounter.GetSolidG(finalCounters)); break;
						case ChannelType.Blue:  OutputSolid(P3dChannelCounter.GetSolidB(finalCounters)); break;
						case ChannelType.Alpha: OutputSolid(P3dChannelCounter.GetSolidA(finalCounters)); break;

						case ChannelType.InverseAlpha: OutputSolid(P3dChannelCounter.GetTotal(finalCounters) - P3dChannelCounter.GetSolidA(finalCounters)); break;
					}
				}
				break;
			}
		}

		private void OutputRatio(float ratio)
		{
			var percentage = Mathf.Clamp01(ratio) * 100.0f;

			if (round >= 0)
			{
				var mult = System.Math.Pow(10.0, round);

				percentage = (float)(System.Math.Truncate(percentage * mult) / mult);
			}

			cachedText.text = string.Format(format, percentage);
		}

		private void OutputSolid(long solid)
		{
			cachedText.text = string.Format(format, solid);
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dChannelCounterText))]
	public class P3dChannelCounterText_Editor : P3dEditor<P3dChannelCounterText>
	{
		protected override void OnInspector()
		{
			Draw("counters", "This allows you to specify the counters that will be used.\n\nZero = All and and enabled counters in the scene.");

			Separator();

			Draw("channel", "This allows you to choose which channel will be output to the UI Text.");
			Draw("output", "This allows you to choose which value will be output to the UI Text.");
			if (Any(t => t.Output == P3dChannelCounterText.OutputType.Percentage))
			{
				Draw("round", "This allows you to round the output value when using <b>Format = Percentage</b>.\n\n1 = One decimal place.");
			}
			Draw("format", "This allows you to set the format of the text, where the {0} token will contain the number.");
		}
	}
}
#endif