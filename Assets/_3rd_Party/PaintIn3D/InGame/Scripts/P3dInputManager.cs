using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This component converts mouse and touch inputs into a single interface.</summary>
	public class P3dInputManager
	{
		public class Finger
		{
			public int     Index;
			public float   Pressure;
			public bool    LastSet;
			public bool    Set;
			public Vector2 PositionA;
			public Vector2 PositionB;
			public Vector2 PositionC;
			public Vector2 PositionD;
			public List<Vector2> SmoothPositions = new List<Vector2>();

			public bool Down
			{
				get
				{
					return Set == true && LastSet == false;
				}
			}

			public bool Up
			{
				get
				{
					return Set == false && LastSet == true;
				}
			}
		}

		private static List<RaycastResult> tempRaycastResults = new List<RaycastResult>(10);

		private static PointerEventData tempPointerEventData;

		private static EventSystem tempEventSystem;

		private List<Finger> fingers = new List<Finger>();

		private static Stack<Finger> pool = new Stack<Finger>();

		public static float ScaleFactor
		{
			get
			{
				var dpi = Screen.dpi;

				if (dpi <= 0)
				{
					dpi = 200.0f;
				}

				return 200.0f / dpi;
			}
		}

		public List<Finger> Fingers
		{
			get
			{
				return fingers;
			}
		}

		public Vector2 GetAverageDeltaScaled()
		{
			var total = Vector2.zero;
			var count = 0;

			for (var i = fingers.Count - 1; i >= 0; i--)
			{
				var finger = fingers[i];

				total += finger.PositionA - finger.PositionB;
				count += 1;
			}

			if (count > 0)
			{
				total *= ScaleFactor;
				total /= count;

			}

			return total;
		}

		public static bool PointOverGui(Vector2 screenPosition)
		{
			return RaycastGui(screenPosition).Count > 0;
		}

		public static List<RaycastResult> RaycastGui(Vector2 screenPosition)
		{
			return RaycastGui(screenPosition, 1 << 5);
		}

		public static List<RaycastResult> RaycastGui(Vector2 screenPosition, LayerMask layerMask)
		{
			tempRaycastResults.Clear();

			var currentEventSystem = EventSystem.current;

			if (currentEventSystem != null)
			{
				// Create point event data for this event system?
				if (currentEventSystem != tempEventSystem)
				{
					tempEventSystem = currentEventSystem;

					if (tempPointerEventData == null)
					{
						tempPointerEventData = new PointerEventData(tempEventSystem);
					}
					else
					{
						tempPointerEventData.Reset();
					}
				}

				// Raycast event system at the specified point
				tempPointerEventData.position = screenPosition;

				currentEventSystem.RaycastAll(tempPointerEventData, tempRaycastResults);

				// Loop through all results and remove any that don't match the layer mask
				if (tempRaycastResults.Count > 0)
				{
					for (var i = tempRaycastResults.Count - 1; i >= 0; i--)
					{
						var raycastResult = tempRaycastResults[i];
						var raycastLayer  = 1 << raycastResult.gameObject.layer;

						if ((raycastLayer & layerMask) == 0)
						{
							tempRaycastResults.RemoveAt(i);
						}
					}
				}
			}

			return tempRaycastResults;
		}

		public void Update(KeyCode key)
		{
			// Discard old fingers that went up
			for (var i = fingers.Count - 1; i >= 0; i--)
			{
				var finger = fingers[i];

				if (finger.Up == true)
				{
					fingers.RemoveAt(i); pool.Push(finger);
				}
				else
				{
					finger.LastSet = finger.Set;
					finger.Set     = false;
				}
			}

			// Update real fingers
			if (Input.touchCount > 0)
			{
				for (var i = 0; i < Input.touchCount; i++)
				{
					var touch = Input.GetTouch(i);

					if (touch.phase == TouchPhase.Began)
					{
						CreateFinger(touch.fingerId, touch.position, touch.pressure);
					}
					else
					{
						UpdateFinger(touch.fingerId, touch.position, touch.pressure, touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary);
					}
				}
			}
			// If there are no real touches, simulate some from the mouse?
			else
			{
				var mousePosition = (Vector2)Input.mousePosition;

				if (Input.GetKeyDown(key) == true)
				{
					CreateFinger(-1, mousePosition, 1.0f);
				}
				else
				{
					UpdateFinger(-1, mousePosition, 1.0f, Input.GetKey(key) == true);
				}
			}
		}

		private void CreateFinger(int index, Vector2 screenPosition, float pressure)
		{
			if (PointOverGui(screenPosition) == false)
			{
				var finger = pool.Count > 0 ? pool.Pop() : new Finger();

				finger.Index     = index;
				finger.Pressure  = pressure;
				finger.LastSet   = false;
				finger.Set       = true;
				finger.PositionA = screenPosition;
				finger.PositionB = screenPosition;
				finger.PositionC = screenPosition;
				finger.PositionD = screenPosition;

				finger.SmoothPositions.Clear();

				finger.SmoothPositions.Add(finger.PositionB);

				fingers.Add(finger);
			}
		}

		private void UpdateFinger(int index, Vector2 screenPosition, float pressure, bool set)
		{
			for (var i = fingers.Count - 1; i >= 0; i--)
			{
				var finger = fingers[i];

				if (finger.Index == index)
				{
					finger.Pressure  = pressure;
					finger.Set       = set;
					finger.PositionD = finger.PositionC;
					finger.PositionC = finger.PositionB;
					finger.PositionB = finger.PositionA;
					finger.PositionA = screenPosition;

					finger.SmoothPositions.Clear();

					finger.SmoothPositions.Add(finger.PositionC);

					if (set == true)
					{
						var steps = Mathf.FloorToInt(Vector2.Distance(finger.PositionB, finger.PositionC));
						var step  = P3dHelper.Reciprocal(steps);

						for (var j = 1; j <= steps; j++)
						{
							var head = Hermite(finger.PositionD, finger.PositionC, finger.PositionB, finger.PositionA, j * step);

							finger.SmoothPositions.Add(head);
						}
					}
					else
					{
						finger.SmoothPositions.Add(finger.PositionB);
						finger.SmoothPositions.Add(finger.PositionA);
					}

					break;
				}
			}
		}

		private static Vector2 Hermite(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
		{
			var mu2 = t * t;
			var mu3 = mu2 * t;
			var x   = HermiteInterpolate(a.x, b.x, c.x, d.x, t, mu2, mu3);
			var y   = HermiteInterpolate(a.y, b.y, c.y, d.y, t, mu2, mu3);

			return new Vector2(x, y);
		}

		private static float HermiteInterpolate(float y0,float y1, float y2,float y3, float mu, float mu2, float mu3)
		{
			var m0 = (y1 - y0) * 0.5f + (y2 - y1) * 0.5f;
			var m1 = (y2 - y1) * 0.5f + (y3 - y2) * 0.5f;
			var a0 =  2.0f * mu3 - 3.0f * mu2 + 1.0f;
			var a1 =         mu3 - 2.0f * mu2 + mu;
			var a2 =         mu3 -        mu2;
			var a3 = -2.0f * mu3 + 3.0f * mu2;

			return(a0*y1+a1*m0+a2*m1+a3*y2);
		}
	}
}