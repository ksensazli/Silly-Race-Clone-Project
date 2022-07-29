using System.Collections.Generic;
using UnityEngine;

namespace PaintIn3D
{
	/// <summary>This class manages the fill painting command.</summary>
	public class P3dCommandFill : P3dCommand
	{
		public Texture Texture;
		public Color   Color;
		public float   Opacity;
		public float   Minimum;
		
		public static P3dCommandFill Instance = new P3dCommandFill();

		private static Stack<P3dCommandFill> pool = new Stack<P3dCommandFill>();

		private static Material[] cachedMaterials;

		public override bool RequireMesh { get { return false; } }

		static P3dCommandFill()
		{
			cachedMaterials = P3dShader.BuildMaterialsBlendModes("Hidden/Paint in 3D/Fill");
		}

		public static RenderTexture Blit(RenderTexture main, P3dBlendMode blendMode, Texture texture, Color color, float opacity, float minimum)
		{
			var swap = P3dHelper.GetRenderTexture(main.descriptor);

			Blit(ref main, ref swap, blendMode, texture, color, opacity, minimum);

			P3dHelper.ReleaseRenderTexture(swap);

			return main;
		}

		public static void Blit(ref RenderTexture main, ref RenderTexture swap, P3dBlendMode blendMode, Texture texture, Color color, float opacity, float minimum)
		{
			Instance.SetMaterial(blendMode, texture, color, opacity, minimum);

			Instance.Apply(main);

			P3dHelper.Blit(swap, Instance.Material);

			P3dHelper.Swap(ref main, ref swap);
		}

		public override void Apply()
		{
			Blend.Apply(Material);

			Material.SetTexture(P3dShader._Texture, Texture);
			Material.SetColor(P3dShader._Color, Color);
			Material.SetFloat(P3dShader._Opacity, Opacity);
			Material.SetVector(P3dShader._Minimum, new Vector4(Minimum, Minimum, Minimum, Minimum));
		}

		public override void Pool()
		{
			pool.Push(this);
		}

		public override void Transform(Matrix4x4 posMatrix, Matrix4x4 rotMatrix)
		{
		}

		public override P3dCommand SpawnCopy()
		{
			var command = SpawnCopy(pool);

			command.Texture = Texture;
			command.Color   = Color;
			command.Opacity = Opacity;
			command.Minimum = Minimum;

			return command;
		}

		public void SetMaterial(P3dBlendMode blendMode, Texture texture, Color color, float opacity, float minimum)
		{
			Blend    = blendMode;
			Material = cachedMaterials[blendMode];
			Texture  = texture;
			Color    = color;
			Opacity  = opacity;
			Minimum  = minimum;
		}
	}
}