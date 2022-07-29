using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dHitScreen")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Hit/Hit Screen")]
	public class P3dHitScreen : P3dConnectablePoints
	{
		// This stores extra information for each finger unique to this component
		class Link
		{
			public P3dInputManager.Finger Finger;
			public float                  Distance;
		}

		public enum OrientationType
		{
			WorldUp,
			CameraUp
		}

		public enum NormalType
		{
			HitNormal,
			RayDirection,
			CameraDirection
		}

		/// <summary>Orient to a specific camera?
		/// None = MainCamera.</summary>
		public Camera Camera { set { _camera = value; } get { return _camera; } } [SerializeField] private Camera _camera;

		/// <summary>If you want the paint to continuously apply while moving the mouse, this allows you to set how many pixels are between each step (0 = no drag).</summary>
		public float Spacing { set { spacing = value; } get { return spacing; } } [SerializeField] private float spacing = 5.0f;

		/// <summary>The layers you want the raycast to hit.</summary>
		public LayerMask Layers { set { layers = value; } get { return layers; } } [SerializeField] private LayerMask layers = Physics.DefaultRaycastLayers;

		/// <summary>If you want painting to require a specific key on desktop platforms, then you can specify it here.</summary>
		public KeyCode Key { set { key = value; } get { return key; } } [SerializeField] private KeyCode key = KeyCode.Mouse0;

		/// <summary>How should the hit point be oriented?
		/// WorldUp = It will be rotated to the normal, where the up vector is world up.
		/// CameraUp = It will be rotated to the normal, where the up vector is world up.</summary>
		public OrientationType Orientation { set { orientation = value; } get { return orientation; } } [SerializeField] private OrientationType orientation = OrientationType.CameraUp;

		/// <summary>Which normal should the hit point rotation be based on?</summary>
		public NormalType Normal { set { normal = value; } get { return normal; } } [SerializeField] private NormalType normal = NormalType.CameraDirection;

		/// <summary>If you want the raycast hit point to be offset from the surface a bit, this allows you to set by how much in world space.</summary>
		public float Offset { set { offset = value; } get { return offset; } } [SerializeField] private float offset;

		/// <summary>If you want the hit point to be offset upwards when using touch input, this allows you to specify the physical distance the hit will be offset by on the screen. This is useful if you find paint hard to see because it's underneath your finger.</summary>
		public float TouchOffset { set { touchOffset = value; } get { return touchOffset; } } [SerializeField] private float touchOffset;

		/// <summary>Show a painting preview under the mouse?</summary>
		public bool ShowPreview { set { showPreview = value; } get { return showPreview; } } [SerializeField] private bool showPreview = true;

		/// <summary>This allows you to override the order this paint gets applied to the object during the current frame.</summary>
		public int Priority { set { priority = value; } get { return priority; } } [SerializeField] private int priority;

		/// <summary>Should painting triggered from this component be eligible for being undone?</summary>
		public bool StoreStates { set { storeStates = value; } get { return storeStates; } } [SerializeField] private bool storeStates = true;

		[System.NonSerialized]
		private List<Link> links = new List<Link>();

		[System.NonSerialized]
		private P3dInputManager inputManager = new P3dInputManager();

		protected void LateUpdate()
		{
			inputManager.Update(key);

			// Use mouse hover preview?
			if (showPreview == true)
			{
				if (Input.touchCount == 0 && Input.GetKey(key) == false && P3dInputManager.PointOverGui(Input.mousePosition) == false)
				{
					PaintAt(Input.mousePosition, true, 1.0f, this);
				}
				else
				{
					BreakHits(this);
				}
			}

			for (var i = inputManager.Fingers.Count - 1; i >= 0; i--)
			{
				var finger = inputManager.Fingers[i];
				var down   = finger.Down;
				var up     = finger.Up;

				Paint(finger, down, up);
			}
		}

		private void Paint(P3dInputManager.Finger finger, bool down, bool up)
		{
			var link = GetLink(finger);

			if (spacing > 0.0f)
			{
				var tail = finger.SmoothPositions[0];

				if (down == true)
				{
					link.Distance = 0.0f;

					if (storeStates == true)
					{
						P3dStateManager.StoreAllStates();
					}

					PaintAt(tail, false, finger.Pressure, link);
				}

				for (var i = 1; i < finger.SmoothPositions.Count; i++)
				{
					var head  = finger.SmoothPositions[i];
					var dist  = Vector2.Distance(tail, head);
					var steps = Mathf.FloorToInt((link.Distance + dist) / spacing);

					for (var j = 0; j < steps; j++)
					{
						var remainder = spacing - link.Distance;

						tail = Vector2.MoveTowards(tail, head, remainder);

						PaintAt(tail, false, finger.Pressure, link);

						dist -= remainder;

						link.Distance = 0.0f;
					}

					link.Distance += dist;

					tail = head;
				}
			}
			else
			{
				if (showPreview == true)
				{
					if (up == true)
					{
						if (storeStates == true)
						{
							P3dStateManager.StoreAllStates();
						}

						PaintAt(finger.PositionA, false, finger.Pressure, link);
					}
					else
					{
						PaintAt(finger.PositionA, true, finger.Pressure, link);
					}
				}
				else if (down == true)
				{
					if (storeStates == true)
					{
						P3dStateManager.StoreAllStates();
					}

					PaintAt(finger.PositionA, false, finger.Pressure, link);
				}
			}

			if (up == true)
			{
				BreakHits(link);
			}
		}

		private void PaintAt(Vector2 screenPosition, bool preview, float pressure, object owner)
		{
			var camera = P3dHelper.GetCamera(_camera);

			if (camera != null)
			{
				if (touchOffset != 0.0f && Input.touchCount > 0)
				{
					screenPosition.y += touchOffset * P3dInputManager.ScaleFactor;
				}

				var ray = camera.ScreenPointToRay(screenPosition);
				var hit = default(RaycastHit);

				if (Physics.Raycast(ray, out hit, float.PositiveInfinity, layers) == true)
				{
					var finalNormal = default(Vector3);

					switch (normal)
					{
						case NormalType.HitNormal: finalNormal = hit.normal; break;
						case NormalType.RayDirection: finalNormal = -ray.direction; break;
						case NormalType.CameraDirection: finalNormal = -camera.transform.forward; break;
					}

					var finalUp       = orientation == OrientationType.CameraUp ? camera.transform.up : Vector3.up;
					var finalPosition = hit.point + hit.normal * offset;
					var finalRotation = Quaternion.LookRotation(-finalNormal, finalUp);

					SubmitPoint(preview, priority, pressure, finalPosition, finalRotation, owner);

					hitCache.InvokeRaycast(gameObject, preview, priority, pressure, hit, finalRotation);

					return;
				}
			}

			BreakHits(owner);
		}

		private Link GetLink(P3dInputManager.Finger finger)
		{
			for (var i = links.Count - 1; i >= 0; i--)
			{
				var link = links[i];

				if (link.Finger == finger)
				{
					return link;
				}
			}

			var newLink = new Link();

			newLink.Finger = finger;

			links.Add(newLink);

			return newLink;
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dHitScreen))]
	public class P3dHitScreen_Editor : P3dConnectablePoints_Editor<P3dHitScreen>
	{
		protected override void OnInspector()
		{
			Draw("_camera", "Orient to a specific camera?\n\nNone = MainCamera.");
			BeginError(Any(t => t.Layers == 0));
				Draw("layers", "The layers you want the raycast to hit.");
			EndError();
			Draw("spacing", "The time in seconds between each raycast.\n\n0 = every frame\n\n-1 = manual only");
			Draw("key", "If you want painting to require a specific key on desktop platforms, then you can specify it here.");

			Separator();

			Draw("orientation", "How should the hit point be oriented?\n\nNone = It will be treated as a point with no rotation.\n\nWorldUp = It will be rotated to the normal, where the up vector is world up.\n\nCameraUp = It will be rotated to the normal, where the up vector is world up.");
			Draw("normal", "Which normal should the hit point rotation be based on?");
			Draw("offset", "If you want the raycast hit point to be offset from the surface a bit, this allows you to set by how much in world space.");
			Draw("touchOffset", "If you want the hit point to be offset upwards when using touch input, this allows you to specify the physical distance the hit will be offset by on the screen. This is useful if you find paint hard to see because it's underneath your finger.");
			Draw("storeStates", "Should painting triggered from this component be eligible for being undone?");

			base.OnInspector();

			Separator();

			Draw("showPreview", "Should the applied paint be applied as a preview?");
			Draw("priority", "This allows you to override the order this paint gets applied to the object during the current frame.");

			Separator();

			Target.HitCache.Inspector(Target.gameObject, true, Target.ConnectHits, false, true, true);
		}
	}
}
#endif