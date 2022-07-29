using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component will output the total pixels for the specified team to a UI Text component.</summary>
	[RequireComponent(typeof(Text))]
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dColorCounterText")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Color Counter Text")]
	public class P3dColorCounterText : MonoBehaviour
	{
		/// <summary>This allows you to set which team will be handled by this component.</summary>
		public P3dColor Color { set { color = value; } get { return color; } } [SerializeField] private P3dColor color;

		/// <summary>This allows you to set the format of the team text.</summary>
		public string Format { set { format = value; } get { return format; } } [Multiline] [SerializeField] private string format = "{TEAM} = {TOTAL} : {RATIO}";

		[System.NonSerialized]
		private Text cachedText;

		protected virtual void OnEnable()
		{
			cachedText = GetComponent<Text>();
		}

		protected virtual void Update()
		{
			if (color != null)
			{
				var final = format;

				final = final.Replace("{TEAM}", color.name);
				final = final.Replace("{TOTAL}", color.Solid.ToString());
				final = final.Replace("{RATIO}", color.Ratio.ToString());

				cachedText.text = final;
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dColorCounterText))]
	public class P3dTeamText_Editor : P3dEditor<P3dColorCounterText>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Color == null));
				Draw("color");
			EndError();
			Draw("format");
		}
	}
}
#endif