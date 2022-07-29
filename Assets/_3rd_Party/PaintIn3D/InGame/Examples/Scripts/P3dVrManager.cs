using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D.Examples
{
	/// <summary>This component attached the current GameObject to a tracked hand.</summary>
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dVrManager")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Examples/VR Manager")]
	public class P3dVrManager : P3dLinkedBehaviour<P3dVrManager>
	{
		class SimulatedState
		{
			public XRNode     Node;
			public bool       Set;
			public Vector3    Position;
			public Quaternion Rotation = Quaternion.identity;

			public SimulatedState(XRNode newNode)
			{
				Node = newNode;
			}
		}

		/// <summary>This key allows you to reset the VR orientation.</summary>
		public KeyCode RecenterKey { set { recenterKey = value; } get { return recenterKey; } } [SerializeField] private KeyCode recenterKey = KeyCode.Space;

		/// <summary>How far you must press the VR buttons for them to be consdered pressed.</summary>
		public float Tolerance { set { tolerance = value; } get { return tolerance; } } [SerializeField] [Range(0.001f, 0.999f)] private float tolerance = 0.1f;

		/// <summary>The default distance in world space a hand must be to grab a tool.</summary>
		public float GrabDistance { set { grabDistance = value; } get { return grabDistance; } } [SerializeField] private float grabDistance = 0.3f;

		/// <summary>This key allows you to simulate a left hand VR trigger.</summary>
		public KeyCode SimulatedLeftTrigger { set { simulatedLeftTrigger = value; } get { return simulatedLeftTrigger; } } [SerializeField] private KeyCode simulatedLeftTrigger = KeyCode.Mouse0;

		/// <summary>This key allows you to simulate a left hand VR grip.</summary>
		public KeyCode SimulatedLeftGrip { set { simulatedLeftGrip = value; } get { return simulatedLeftGrip; } } [SerializeField] private KeyCode simulatedLeftGrip = KeyCode.LeftControl;

		/// <summary>This key allows you to simulate a right hand VR trigger.</summary>
		public KeyCode SimulatedRightTrigger { set { simulatedRightTrigger = value; } get { return simulatedRightTrigger; } } [SerializeField] private KeyCode simulatedRightTrigger = KeyCode.Mouse1;

		/// <summary>This key allows you to simulate a right hand VR grip.</summary>
		public KeyCode SimulatedRightGrip { set { simulatedRightGrip = value; } get { return simulatedRightGrip; } } [SerializeField] private KeyCode simulatedRightGrip = KeyCode.RightControl;

		/// <summary>When simulating a VR tool, it will be offset by this euler rotation.</summary>
		public Vector3 SimulatedTilt { set { simulatedTilt = value; } get { return simulatedTilt; } } [SerializeField] private Vector3 simulatedTilt = new Vector3(0.0f, -15.0f, 0.0f);

		/// <summary>When simulating a VR tool, it will be offset by this local position.</summary>
		public Vector3 SimulatedOffset { set { simulatedOffset = value; } get { return simulatedOffset; } } [SerializeField] private Vector3 simulatedOffset = new Vector3(0.0f, 0.0f, -0.2f);

		/// <summary>When simulating a VR tool, it will be moved away from the hit surface by this.</summary>
		public float SimulatedDistanceMax { set { simulatedReach = value; } get { return simulatedReach; } } [SerializeField] private float simulatedReach = 1.0f;

		/// <summary>The simulated left VR eye will be offset this much.</summary>
		public Vector3 SimulatedEyeOffset { set { simulatedEyeOffset = value; } get { return simulatedEyeOffset; } } [SerializeField] private Vector3 simulatedEyeOffset = new Vector3(-0.0325f, 0.0f, 0.0f);

		/// <summary>When simulating a VR tool, this will control how much the hit surface normal influences the tool rotation.</summary>
		public float SimulatedNormalInfluence { set { simulatedNormalInfluence = value; } get { return simulatedNormalInfluence; } } [Range(0.0f, 1.0f)] [SerializeField] private float simulatedNormalInfluence = 0.25f;
		
		private SimulatedState[] simulatedStates = new SimulatedState[]
			{
				new SimulatedState(XRNode.LeftEye),
				new SimulatedState(XRNode.RightEye),
				new SimulatedState(XRNode.CenterEye),
				new SimulatedState(XRNode.Head),
				new SimulatedState(XRNode.LeftHand),
				new SimulatedState(XRNode.RightHand)
			};

		private float hitDistance;

		private Quaternion hitRotation = Quaternion.identity;
		
		public float LeftTrigger;
		public float RightTrigger;
		public float LeftGrip;
		public float RightGrip;

		public float PrevLeftTrigger;
		public float PrevRightTrigger;
		public float PrevLeftGrip;
		public float PrevRightGrip;

		private static List<XRNodeState> states = new List<XRNodeState>();

		private static List<P3dVrTool> tempTools = new List<P3dVrTool>();

		public bool IsSimulation
		{
			get
			{
				return XRSettings.enabled == false;
			}
		}

		public bool GetTrigger(XRNode node)
		{
			switch (node)
			{
				case XRNode.LeftHand: return LeftTrigger >= tolerance;
				case XRNode.RightHand: return RightTrigger >= tolerance;
			}

			return false;
		}

		public bool GetTriggerPressed(XRNode node)
		{
			switch (node)
			{
				case XRNode.LeftHand: return LeftTrigger >= tolerance && PrevLeftTrigger < tolerance;
				case XRNode.RightHand: return RightTrigger >= tolerance && PrevRightTrigger < tolerance;
			}

			return false;
		}

		public bool GetTriggerReleased(XRNode node)
		{
			switch (node)
			{
				case XRNode.LeftHand: return LeftTrigger < tolerance && PrevLeftTrigger >= tolerance;
				case XRNode.RightHand: return RightTrigger < tolerance && PrevRightTrigger >= tolerance;
			}

			return false;
		}
		
		public bool GetGrip(XRNode node)
		{
			switch (node)
			{
				case XRNode.LeftHand: return LeftGrip >= tolerance;
				case XRNode.RightHand: return RightGrip >= tolerance;
			}

			return false;
		}

		public bool GetGripPressed(XRNode node)
		{
			switch (node)
			{
				case XRNode.LeftHand: return LeftGrip >= tolerance && PrevLeftGrip < tolerance;
				case XRNode.RightHand: return RightGrip >= tolerance && PrevRightGrip < tolerance;
			}

			return false;
		}

		public bool GetGripReleased(XRNode node)
		{
			switch (node)
			{
				case XRNode.LeftHand: return LeftGrip < tolerance && PrevLeftGrip >= tolerance;
				case XRNode.RightHand: return RightGrip < tolerance && PrevRightGrip >= tolerance;
			}

			return false;
		}

		public XRNode GetClosestNode(Vector3 point, float maximumDistance)
		{
			var bestNode     = (XRNode)(-1);
			var bestDistance = maximumDistance;
			var position     = default(Vector3);

			if (TryGetPosition(XRNode.LeftHand, ref position) == true)
			{
				var distance = Vector3.Distance(point, position);

				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestNode     = XRNode.LeftHand;
				}
			}

			if (TryGetPosition(XRNode.RightHand, ref position) == true)
			{
				var distance = Vector3.Distance(point, position);

				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestNode     = XRNode.RightHand;
				}
			}

			return bestNode;
		}

		private void SetSimulatedState(XRNode node, Vector3 position, Quaternion rotation)
		{
			foreach (var simulatedState in simulatedStates)
			{
				if (simulatedState.Node == node)
				{
					simulatedState.Set      = true;
					simulatedState.Position = position;
					simulatedState.Rotation = rotation;

					return;
				}
			}
		}

		public bool TryGetPosition(XRNode node, ref Vector3 position)
		{
			if (IsSimulation == true)
			{
				foreach (var simulatedState in simulatedStates)
				{
					if (simulatedState.Node == node)
					{
						if (simulatedState.Set == false)
						{
							return false;
						}

						position = simulatedState.Position;

						return true;
					}
				}
			}
			else
			{
				InputTracking.GetNodeStates(states);

				foreach (var state in states)
				{
					if (state.nodeType == node)
					{
						return state.TryGetPosition(out position);
					}
				}
			}

			return false;
		}

		public bool TryGetRotation(XRNode node, ref Quaternion rotation)
		{
			if (IsSimulation == true)
			{
				foreach (var simulatedState in simulatedStates)
				{
					if (simulatedState.Node == node)
					{
						if (simulatedState.Set == false)
						{
							return false;
						}

						rotation = simulatedState.Rotation;

						return true;
					}
				}
			}
			else
			{
				InputTracking.GetNodeStates(states);

				foreach (var state in states)
				{
					if (state.nodeType == node)
					{
						return state.TryGetRotation(out rotation);
					}
				}
			}

			return false;
		}

		protected virtual void Start()
		{
			Recenter();

			hitDistance = simulatedReach * 0.25f;
		}

		protected virtual void Update()
		{
			PrevLeftTrigger  = LeftTrigger;
			PrevLeftGrip     = LeftGrip;
			PrevRightTrigger = RightTrigger;
			PrevRightGrip    = RightGrip;
			
			LeftTrigger = IsSimulation == true ? Input.GetKey(simulatedLeftTrigger) ? 1.0f : 0.0f : Input.GetAxisRaw("VR Left Trigger");
			LeftGrip = IsSimulation == true ? Input.GetKey(simulatedLeftGrip) ? 1.0f : 0.0f : Input.GetAxisRaw("VR Left Grip");

			RightTrigger = IsSimulation == true ? Input.GetKey(simulatedRightTrigger) ? 1.0f : 0.0f : Input.GetAxisRaw("VR Right Trigger");
			RightGrip = IsSimulation == true ? Input.GetKey(simulatedRightGrip) ? 1.0f : 0.0f : Input.GetAxisRaw("VR Right Grip");

			if (Input.GetKeyDown(recenterKey) == true)
			{
				Recenter();
			}

			var camera = P3dHelper.GetCamera();

			if (camera != null)
			{
				var ray = camera.ScreenPointToRay(Input.mousePosition);
				var hit = default(RaycastHit);
				var cam = camera.transform.rotation;

				if (Physics.Raycast(ray, out hit, simulatedReach) == true)
				{
					hitDistance = hit.distance;
					hitRotation = Quaternion.Inverse(cam) * Quaternion.LookRotation(-hit.normal);
				}

				var leftHandRot = Quaternion.Slerp(cam, cam * hitRotation, simulatedNormalInfluence) * Quaternion.Euler(simulatedTilt.x, -simulatedTilt.y, simulatedTilt.z);
				var leftHandPos = ray.GetPoint(hitDistance) + leftHandRot * new Vector3(simulatedOffset.x, simulatedOffset.y, simulatedOffset.z);

				SetSimulatedState(XRNode.LeftHand, leftHandPos, leftHandRot);

				var rightHandRot = Quaternion.Slerp(cam, cam * hitRotation, simulatedNormalInfluence) * Quaternion.Euler(simulatedTilt.x, simulatedTilt.y, simulatedTilt.z);
				var rightHandPos = ray.GetPoint(hitDistance) + rightHandRot * new Vector3(simulatedOffset.x, -simulatedOffset.y, simulatedOffset.z);

				SetSimulatedState(XRNode.RightHand, rightHandPos, rightHandRot);

				SetSimulatedState(XRNode.Head, camera.transform.position, camera.transform.rotation);

				SetSimulatedState(XRNode.CenterEye, camera.transform.position, camera.transform.rotation);

				SetSimulatedState(XRNode.LeftEye, camera.transform.TransformPoint(simulatedEyeOffset.x, simulatedEyeOffset.y, simulatedEyeOffset.z), camera.transform.rotation);

				SetSimulatedState(XRNode.RightEye, camera.transform.TransformPoint(-simulatedEyeOffset.x, simulatedEyeOffset.y, simulatedEyeOffset.z), camera.transform.rotation);
			}

			for (var i = 0; i <= 8; i++)
			{
				UpdateTools((XRNode)i);
			}
		}

		private void UpdateTools(XRNode node)
		{
			P3dVrTool.GetTools(node, ref tempTools);

			foreach (var tool in tempTools)
			{
				if (tool != null && tool.Node == node)
				{
					tool.UpdateGripped(this);
				}
			}
		}

		[ContextMenu("Recenter")]
		public void Recenter()
		{
			if (XRSettings.enabled == true)
			{
				InputTracking.Recenter();
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dVrManager))]
	public class P3dVrManager_Editor : P3dEditor<P3dVrManager>
	{
		protected override void OnInspector()
		{
			if (XRSettings.enabled == false)
			{
				EditorGUILayout.HelpBox("VR is disabled in your project settings, so simulated fingers will be used. If you have a VR device then you can enable it.", MessageType.Warning);
			}

			Draw("recenterKey", "This key allows you to reset the VR orientation.");
			Draw("tolerance", "How far you must press the VR buttons for them to be consdered pressed.");
			Draw("grabDistance", "The default distance in world space a hand must be to grab a tool.");

			Separator();

			Draw("simulatedLeftTrigger", "This key allows you to simulate a left hand VR trigger.");
			Draw("simulatedLeftGrip", "This key allows you to simulate a left hand VR grip.");
			Draw("simulatedRightTrigger", "This key allows you to simulate a right hand VR trigger.");
			Draw("simulatedRightGrip", "This key allows you to simulate a right hand VR grip.");

			Separator();

			Draw("simulatedTilt", "When simulating a VR tool, it will be offset by this euler rotation.");
			Draw("simulatedOffset", "When simulating a VR tool, it will be offset by this local position.");
			Draw("simulatedReach", "When simulating a VR tool, it will be moved away from the hit surface by this.");
			Draw("simulatedEyeOffset", "The simulated left VR eye will be offset this much.");
			Draw("simulatedNormalInfluence", "When simulating a VR tool, this will control how much the hit surface normal influences the tool rotation.");

			if (Button("Map Controllers") == true)
			{
				MapControllers();
			}
		}

		private static void MapControllers()
		{
			var inputManagers = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");

			if (inputManagers != null && inputManagers.Length > 0)
			{
				var inputManager = new SerializedObject(inputManagers[0]);
				var axes         = inputManager.FindProperty("m_Axes");

				if (axes != null)
				{
					MapController(axes, "VR Left Trigger", 8);
					MapController(axes, "VR Right Trigger", 9);
					MapController(axes, "VR Left Grip", 10);
					MapController(axes, "VR Right Grip",11);
				}

				inputManager.ApplyModifiedProperties();

				Debug.Log("VR Controls Mapped!");
			}
		}

		private static SerializedProperty GetAxisIndex(SerializedProperty axes, string name)
		{
			for (var i = 0; i < axes.arraySize; i++)
			{
				var axis = axes.GetArrayElementAtIndex(i);

				if (axis.FindPropertyRelative("m_Name").stringValue == name)
				{
					return axis;
				}
			}

			var index = axes.arraySize;

			axes.InsertArrayElementAtIndex(index);

			return axes.GetArrayElementAtIndex(index);
		}

		private static void MapController(SerializedProperty axes, string name, int axisId)
		{
			var axis = GetAxisIndex(axes, name);

			axis.FindPropertyRelative("m_Name").stringValue = name;
			axis.FindPropertyRelative("axis").intValue = axisId;
			axis.FindPropertyRelative("type").intValue = 2; // Axis
			axis.FindPropertyRelative("gravity").floatValue = 0;
			axis.FindPropertyRelative("sensitivity").floatValue = 1;
		}
	}
}
#endif