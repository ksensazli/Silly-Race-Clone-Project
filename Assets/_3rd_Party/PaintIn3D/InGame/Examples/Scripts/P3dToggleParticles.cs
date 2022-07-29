using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This component enables or disables the specified ParticleSystem based on mouse or finger presses.</summary>
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dToggleParticles")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/Toggle Particles")]
	public class P3dToggleParticles : MonoBehaviour
	{
		/// <summary>The key that must be held.
		/// None = Mouse or Touch.</summary>
		public KeyCode Key { set { key = value; } get { return key; } } [SerializeField] private KeyCode key;

		/// <summary>The particle system that will be enabled/disabled based on mouse/touch.</summary>
		public ParticleSystem Target { set { target = value; } get { return target; } } [SerializeField] private ParticleSystem target;

		/// <summary>Should painting triggered from this component be eligible for being undone?</summary>
		public bool StoreStates { set { storeStates = value; } get { return storeStates; } } [SerializeField] protected bool storeStates = true;

		[System.NonSerialized]
		private P3dInputManager inputManager = new P3dInputManager();

		protected virtual void LateUpdate()
		{
			inputManager.Update(key);

			if (target != null)
			{
				if (Pressing() == true)
				{
					if (storeStates == true && target.isPlaying == false)
					{
						P3dStateManager.StoreAllStates();
					}

					target.Play();
				}
				else
				{
					target.Stop();
				}
			}
		}

		private bool Pressing()
		{
			return inputManager.Fingers.Count > 0;
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dToggleParticles))]
	public class P3dToggleParticles_Editor : P3dEditor<P3dToggleParticles>
	{
		protected override void OnInspector()
		{
			Draw("key", "The key that must be held.\n\nNone = Mouse or Touch.");
			BeginError(Any(t => t.Target == null));
				Draw("target", "The particle system that will be enabled/disabled based on mouse/touch.");
			EndError();
			Draw("storeStates", "Should painting triggered from this component be eligible for being undone?");
		}
	}
}
#endif