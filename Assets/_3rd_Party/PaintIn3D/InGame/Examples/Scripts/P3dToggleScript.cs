using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component allows you to enable/disable the target component while the specified key is held down.</summary>
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dToggleScript")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Toggle Script")]
	public class P3dToggleScript : MonoBehaviour
	{
		/// <summary>The key that must be held.
		/// None = Mouse or Touch.</summary>
		public KeyCode Key { set { key = value; } get { return key; } } [SerializeField] private KeyCode key;

		/// <summary>The component that will be enabled or disabled.</summary>
		public MonoBehaviour Target { set { target = value; } get { return target; } } [SerializeField] private MonoBehaviour target;

		/// <summary>Should painting triggered from this component be eligible for being undone?</summary>
		public bool StoreStates { set { storeStates = value; } get { return storeStates; } } [SerializeField] protected bool storeStates;

		[System.NonSerialized]
		private P3dInputManager inputManager = new P3dInputManager();

		protected virtual void Update()
		{
			inputManager.Update(key);

			if (target != null)
			{
				if (Pressing() == true)
				{
					if (storeStates == true && target.enabled == false)
					{
						P3dStateManager.StoreAllStates();
					}

					target.enabled = true;
				}
				else
				{
					target.enabled = false;
				}
			}
		}

		private bool Pressing()
		{
			for (var i = 0; i < inputManager.Fingers.Count; i++)
			{
				var finger = inputManager.Fingers[i];

				if (finger.Index >= 0 || finger.Index == -1) // Touch or left click
				{
					return true;
				}
			}

			return false;
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dToggleScript))]
	public class P3dKeyControl_Editor : P3dEditor<P3dToggleScript>
	{
		protected override void OnInspector()
		{
			Draw("key", "The key that must be held.\n\nNone = Mouse or Touch.");
			BeginError(Any(t => t.Target == null));
				Draw("target", "The component that will be enabled or disabled.");
			EndError();
			Draw("storeStates", "Should painting triggered from this component be eligible for being undone?");
		}
	}
}
#endif