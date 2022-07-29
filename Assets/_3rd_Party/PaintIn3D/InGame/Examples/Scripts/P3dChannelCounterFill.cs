using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component fills the attached UI Image based on the total amount of opaque pixels that have been painted in all active and enabled <b>P3dChannelCounter</b> components in the scene.</summary>
	[RequireComponent(typeof(Image))]
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dChannelCounterFill")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Channel Counter Fill")]
	public class P3dChannelCounterFill : MonoBehaviour
	{
		public enum ChannelType
		{
			Red,
			Green,
			Blue,
			Alpha,
			InverseAlpha
		}

		/// <summary>This allows you to specify the counters that will be used.
		/// Zero = All and and enabled counters in the scene.</summary>
		public List<P3dChannelCounter> Counters { get { if (counters == null) counters = new List<P3dChannelCounter>(); return counters; } } [SerializeField] private List<P3dChannelCounter> counters;

		/// <summary>This allows you to choose which channel will be output to the UI Image.</summary>
		public ChannelType Channel { set { channel = value; } get { return channel; } } [SerializeField] private ChannelType channel;

		[System.NonSerialized]
		private Image cachedImage;

		protected virtual void OnEnable()
		{
			cachedImage = GetComponent<Image>();
		}

		protected virtual void Update()
		{
			var finalCounters = counters.Count > 0 ? counters : null;
			var ratioRGBA     = P3dChannelCounter.GetRatioRGBA(finalCounters);
			var amount        = 0.0f;

			switch (channel)
			{
				case ChannelType.Red:   amount = ratioRGBA.x; break;
				case ChannelType.Green: amount = ratioRGBA.y; break;
				case ChannelType.Blue:  amount = ratioRGBA.z; break;
				case ChannelType.Alpha: amount = ratioRGBA.w; break;

				case ChannelType.InverseAlpha: amount = 1.0f - ratioRGBA.w; break;
			}

			cachedImage.fillAmount = Mathf.Clamp01(amount);
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dChannelCounterFill))]
	public class P3dChannelCounterFill_Editor : P3dEditor<P3dChannelCounterFill>
	{
		protected override void OnInspector()
		{
			Draw("counters", "This allows you to specify the counters that will be used.\n\nZero = All and and enabled counters in the scene.");

			Separator();

			Draw("channel", "This allows you to choose which channel will be output to the UI Image.");
		}
	}
}
#endif