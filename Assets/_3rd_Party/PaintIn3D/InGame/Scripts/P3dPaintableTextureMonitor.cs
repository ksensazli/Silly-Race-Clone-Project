using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This base class allows you to quickly create components that listen for changes to the specified P3dPaintableTexture.</summary>
	public abstract class P3dPaintableTextureMonitor : MonoBehaviour
	{
		/// <summary>This is the paintable texture whose pixels we will count.</summary>
		public P3dPaintableTexture PaintableTexture { set { paintableTexture = value; Register(); } get { return paintableTexture; } } [SerializeField] private P3dPaintableTexture paintableTexture;

		[SerializeField]
		private P3dPaintableTexture registeredPaintableTexture;

		/// <summary>This will be true after Register is successfully called.</summary>
		public bool Registered
		{
			get
			{
				return registeredPaintableTexture != null;
			}
		}

		/// <summary>This forces the specified P3dPaintableTexture to be registered.</summary>
		[ContextMenu("Register")]
		public void Register()
		{
			Unregister();

			if (paintableTexture != null)
			{
				paintableTexture.OnModified += HandleModified;

				registeredPaintableTexture = paintableTexture;
			}
		}

		/// <summary>This forces the specified P3dPaintableTexture to be unregistered.</summary>
		[ContextMenu("Unregister")]
		public void Unregister()
		{
			if (registeredPaintableTexture != null)
			{
				registeredPaintableTexture.OnModified -= HandleModified;

				registeredPaintableTexture = null;
			}
		}

		protected virtual void OnEnable()
		{
			Register();
		}

		protected virtual void OnDisable()
		{
			Unregister();
		}

		protected virtual void Start()
		{
			HandleModified(false);
		}

		protected abstract void UpdateMonitor(P3dPaintableTexture paintableTexture, bool preview);

		private void HandleModified(bool preview)
		{
			if (registeredPaintableTexture != null)
			{
				UpdateMonitor(paintableTexture, preview);
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	public class P3dPaintableTextureMonitor_Editor<T> : P3dEditor<T>
		where T : P3dPaintableTextureMonitor
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.PaintableTexture == null));
				if (Draw("paintableTexture", "This is the paintable texture whose pixels we will count.") == true)
				{
					Each(t =>
						{
							if (t.Registered == true)
							{
								t.Register();
							}
						}, true);
				}
			EndError();
		}
	}
}
#endif