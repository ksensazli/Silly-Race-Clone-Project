using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This allows you to paint a sphere at a hit point. A hit point can be found using a companion component like: P3dDragRaycast, P3dOnCollision, P3dOnParticleCollision.</summary>
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dPaintSphere")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Paint/Paint Sphere")]
	public class P3dPaintSphere : MonoBehaviour, IHit, IHitPoint, IHitLine, IHitQuad, IHitRaycast
	{
		public enum RotationType
		{
			World,
			Normal
		}

		public enum PaintType
		{
			In3D,
			OnUV
		}

		/// <summary>Only the P3dModel/P3dPaintable GameObjects whose layers are within this mask will be eligible for painting.</summary>
		public LayerMask Layers { set { layers = value; } get { return layers; } } [SerializeField] private LayerMask layers = -1;

		/// <summary>Only the <b>P3dPaintableTexture</b> components with a matching group will be painted by this component.</summary>
		public P3dGroup Group { set { group = value; } get { return group; } } [SerializeField] private P3dGroup group;

		/// <summary>If this is set, then only the specified P3dModel/P3dPaintable will be painted, regardless of the layer setting.</summary>
		public P3dModel TargetModel { set { targetModel = value; } get { return targetModel; } } [SerializeField] private P3dModel targetModel;

		/// <summary>If this is set, then only the specified P3dPaintableTexture will be painted, regardless of the layer or group setting.</summary>
		public P3dPaintableTexture TargetTexture { set { targetTexture = value; } get { return targetTexture; } } [SerializeField] private P3dPaintableTexture targetTexture;

		/// <summary>This allows you to control how the paint is applied to the texture.
		/// In3D = Paint will be applied seamlessly across multiple objects in world space.
		/// OnUV = Paint will be applied to one object in UV space.</summary>
		public PaintType Paint { set { paint = value; } get { return paint; } } [SerializeField] private PaintType paint;

		/// <summary>This component will paint using this blending mode.
		/// NOTE: See <b>P3dBlendMode</b> documentation for more information.</summary>
		public P3dBlendMode BlendMode { set { blendMode = value; } get { return blendMode; } } [SerializeField] private P3dBlendMode blendMode = P3dBlendMode.AlphaBlend;

		/// <summary>The color of the paint.</summary>
		public Color Color { set { color = value; } get { return color; } } [SerializeField] private Color color = Color.white;

		/// <summary>The opacity of the brush.</summary>
		public float Opacity { set { opacity = value; } get { return opacity; } } [Range(0.0f, 1.0f)] [SerializeField] private float opacity = 1.0f;

		/// <summary>The angle of the paint in degrees.
		/// NOTE: This is only useful if you change the <b>Scale.x/y</b> values.</summary>
		public float Angle { set { angle = value; } get { return angle; } } [Range(-180.0f, 180.0f)] [SerializeField] private float angle;

		/// <summary>If you want to override the scale of the sphere to paint an ellipse, then set the scale here.</summary>
		public Vector3 Scale { set { scale = value; } get { return scale; } } [SerializeField] private Vector3 scale = Vector3.one;

		/// <summary>This allows you to control how the ellipse is rotated.</summary>
		public RotationType RotateTo { set { rotateTo = value; } get { return rotateTo; } } [SerializeField] private RotationType rotateTo;

		/// <summary>The radius of the paint brush.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [SerializeField] private float radius = 0.1f;

		/// <summary>The hardness of the paint brush.</summary>
		public float Hardness { set { hardness = value; } get { return hardness; } } [SerializeField] private float hardness = 1.0f;

		/// <summary>This allows you to apply a tiled detail texture to your decals. This tiling will be applied in world space using triplanar mapping.</summary>
		public Texture TileTexture { set { tileTexture = value; } get { return tileTexture; } } [SerializeField] private Texture tileTexture;

		/// <summary>This allows you to adjust the tiling position + rotation + scale using a <b>Transform</b>.</summary>
		public Transform TileTransform { set { tileTransform = value; } get { return tileTransform; } } [SerializeField] private Transform tileTransform;

		/// <summary>This allows you to control the triplanar influence.
		/// 0 = No influence.
		/// 1 = Full influence.</summary>
		public float TileOpacity { set { tileOpacity = value; } get { return tileOpacity; } } [UnityEngine.Serialization.FormerlySerializedAs("tileBlend")] [Range(0.0f, 1.0f)] [SerializeField] private float tileOpacity = 1.0f;

		/// <summary>This stores a list of all modifiers used to change the way this component applies paint (e.g. <b>P3dModifyColorRandom</b>).</summary>
		public P3dModifierList Modifiers { get { if (modifiers == null) modifiers = new P3dModifierList(); return modifiers; } } [SerializeField] private P3dModifierList modifiers;

		/// <summary>This method multiplies the radius by the specified value.</summary>
		public void IncrementOpacity(float delta)
		{
			opacity = Mathf.Clamp01(opacity + delta);
		}

		/// <summary>This method increments the angle by the specified amount of degrees, and wraps it to the -180..180 range.</summary>
		public void IncrementAngle(float degrees)
		{
			angle = Mathf.Repeat(angle + 180.0f + degrees, 360.0f) - 180.0f;
		}

		/// <summary>This method multiplies the radius by the specified value.</summary>
		public void MultiplyRadius(float multiplier)
		{
			radius *= multiplier;
		}

		/// <summary>This method multiplies the scale by the specified value.</summary>
		public void MultiplyScale(float multiplier)
		{
			scale *= multiplier;
		}

		/// <summary>This method paints all pixels at the specified point using the shape of a sphere.</summary>
		public void HandleHitPoint(bool preview, int priority, float pressure, int seed, Vector3 position, Quaternion rotation)
		{
			if (paint != PaintType.In3D) return;

			if (modifiers != null && modifiers.Count > 0)
			{
				P3dHelper.BeginSeed(seed);
					modifiers.ModifyPosition(ref position, preview, pressure);
				P3dHelper.EndSeed();
			}

			P3dCommandSphere.Instance.SetState(preview, priority);
			P3dCommandSphere.Instance.SetLocation(position);

			var worldSize     = HandleHitCommon(preview, pressure, seed, rotation);
			var worldRadius   = P3dHelper.GetRadius(worldSize);
			var worldPosition = position;

			P3dPaintableManager.SubmitAll(P3dCommandSphere.Instance, worldPosition, worldRadius, layers, group, targetModel, targetTexture);
		}

		/// <summary>This method paints all pixels between the two specified points using the shape of a sphere.</summary>
		public void HandleHitLine(bool preview, int priority, float pressure, int seed, Vector3 position, Vector3 endPosition, Quaternion rotation)
		{
			if (paint != PaintType.In3D) return;

			P3dCommandSphere.Instance.SetState(preview, priority);
			P3dCommandSphere.Instance.SetLocation(position, endPosition);

			var worldSize     = HandleHitCommon(preview, pressure, seed, rotation);
			var worldRadius   = P3dHelper.GetRadius(worldSize, position, endPosition);
			var worldPosition = P3dHelper.GetPosition(position, endPosition);

			P3dPaintableManager.SubmitAll(P3dCommandSphere.Instance, worldPosition, worldRadius, layers, group, targetModel, targetTexture);
		}

		/// <summary>This method paints all pixels between two pairs of points using the shape of a sphere.</summary>
		public void HandleHitQuad(bool preview, int priority, float pressure, int seed, Vector3 position, Vector3 endPosition, Vector3 position2, Vector3 endPosition2, Quaternion rotation)
		{
			if (paint != PaintType.In3D) return;

			P3dCommandSphere.Instance.SetState(preview, priority);
			P3dCommandSphere.Instance.SetLocation(position, endPosition, position2, endPosition2);

			var worldSize     = HandleHitCommon(preview, pressure, seed, rotation);
			var worldRadius   = P3dHelper.GetRadius(worldSize, position, endPosition, position2, endPosition2);
			var worldPosition = P3dHelper.GetPosition(position, endPosition, position2, endPosition2);

			P3dPaintableManager.SubmitAll(P3dCommandSphere.Instance, worldPosition, worldRadius, layers, group, targetModel, targetTexture);
		}

		/// <summary>This method paints the scene using the current component settings at the specified <b>RaycastHit</b>.
		/// NOTE: The <b>rotation</b> argument is in world space, where <b>Quaternion.identity</b> means the paint faces forward on the +Z axis, and up is +Y.</summary>
		public void HandleHitRaycast(bool preview, int priority, float pressure, int seed, RaycastHit hit, Quaternion rotation)
		{
			if (paint == PaintType.In3D) return;

			var model = hit.collider.GetComponent<P3dModel>();

			if (model != null)
			{
				var paintableTextures = P3dPaintableTexture.Filter(model, group);

				for (var i = paintableTextures.Count - 1; i >= 0; i--)
				{
					var paintableTexture = paintableTextures[i];
					var coord            = paintableTexture.GetCoord(ref hit);

					if (modifiers != null && modifiers.Count > 0)
					{
						var position = (Vector3)coord;

						P3dHelper.BeginSeed(seed);
							modifiers.ModifyPosition(ref position, preview, pressure);
						P3dHelper.EndSeed();

						coord = position;
					}

					P3dCommandSphere.Instance.SetState(preview, priority);
					P3dCommandSphere.Instance.SetLocation(coord, false);

					HandleHitCommon(preview, pressure, seed, rotation);

					P3dCommandSphere.Instance.ApplyAspect(paintableTexture.Current);

					P3dPaintableManager.Submit(P3dCommandSphere.Instance, model, paintableTexture);
				}
			}
		}

		private Vector3 HandleHitCommon(bool preview, float pressure, int seed, Quaternion rotation)
		{
			var finalOpacity    = opacity;
			var finalRadius     = radius;
			var finalHardness   = hardness;
			var finalColor      = color;
			var finalAngle      = angle;
			var finalTileMatrix = tileTransform != null ? tileTransform.localToWorldMatrix : Matrix4x4.identity;

			if (modifiers != null && modifiers.Count > 0)
			{
				P3dHelper.BeginSeed(seed);
					modifiers.ModifyColor(ref finalColor, preview, pressure);
					modifiers.ModifyAngle(ref finalAngle, preview, pressure);
					modifiers.ModifyOpacity(ref finalOpacity, preview, pressure);
					modifiers.ModifyRadius(ref finalRadius, preview, pressure);
					modifiers.ModifyHardness(ref finalHardness, preview, pressure);
				P3dHelper.EndSeed();
			}

			var finalSize = scale * finalRadius;

			P3dCommandSphere.Instance.SetShape(rotateTo == RotationType.World ? Quaternion.identity : rotation, finalSize, finalAngle);

			P3dCommandSphere.Instance.SetMaterial(BlendMode, finalHardness, finalColor, finalOpacity, tileTexture, finalTileMatrix, tileOpacity);

			return finalSize;
		}
#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);

			Gizmos.DrawWireSphere(Vector3.zero, radius);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dPaintSphere))]
	public class P3dClickToPaintSphere_Editor : P3dEditor<P3dPaintSphere>
	{
		private bool expandLayers;
		private bool expandGroups;

		protected override void OnInspector()
		{
			BeginError(Any(t => t.Layers == 0 && t.TargetModel == null));
				DrawExpand(ref expandLayers, "layers", "Only the P3dModel/P3dPaintable GameObjects whose layers are within this mask will be eligible for painting.");
			EndError();
			if (expandLayers == true || Any(t => t.TargetModel != null))
			{
				BeginIndent();
					Draw("targetModel", "If this is set, then only the specified P3dModel/P3dPaintable will be painted, regardless of the layer setting.");
				EndIndent();
			}
			DrawExpand(ref expandGroups, "group", "Only the P3dPaintableTexture components with a matching group will be painted by this component.");
			if (expandGroups == true || Any(t => t.TargetTexture != null))
			{
				BeginIndent();
					Draw("targetTexture", "If this is set, then only the specified P3dPaintableTexture will be painted, regardless of the layer or group setting.");
				EndIndent();
			}

			Separator();

			Draw("paint", "This allows you to control how the paint is applied to the texture.\n\nIn3D = Paint will be applied seamlessly across multiple objects in world space.\nOnUV = Paint will be applied to one object in UV space.\n\nNOTE: UV painting only works with components that perform raycasts against non-convex MeshColliders (e.g. P3dHitScreen, P3dHitBetween).");
			Draw("blendMode", "This component will paint using this blending mode.\n\nNOTE: See P3dBlendMode documentation for more information.");
			Draw("color", "The color of the paint.");
			Draw("opacity", "The opacity of the brush.");

			Separator();

			Draw("angle", "The angle of the paint in degrees.\n\nNOTE: This is only useful if you change the Scale.x/y values.");
			Draw("scale", "If you want to override the scale of the sphere to paint an ellipse, then set the scale here.");
			Draw("rotateTo", "This allows you to control how the ellipse is rotated.");
			Draw("radius", "The radius of the paint brush.");
			Draw("hardness", "This allows you to control the sharpness of the near+far depth cut-off point.");

			Separator();

			Draw("tileTexture", "This allows you to apply a tiled detail texture to your decals. This tiling will be applied in world space using triplanar mapping.");
			Draw("tileTransform", "This allows you to adjust the tiling position + rotation + scale using a Transform.");
			Draw("tileOpacity", "This allows you to control the triplanar influence.\n\n0 = No influence.\n\n1 = Full influence.");

			Separator();

			Target.Modifiers.DrawEditorLayout(serializedObject, target, "Color", "Opacity", "Radius", "Hardness", "Position");
		}
	}
}
#endif