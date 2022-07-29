using System.Collections.Generic;
using UnityEngine;

namespace PaintIn3D
{
	/// <summary>This class manages the replace painting command.</summary>
	public class P3dCommandReplace : P3dCommand
	{
		public Texture Texture;
		public Color   Color;

		public static P3dCommandReplace Instance = new P3dCommandReplace();

		private static Stack<P3dCommandReplace> pool = new Stack<P3dCommandReplace>();

		private static Material cachedMaterial;

		public override bool RequireMesh { get { return false; } }

		static P3dCommandReplace()
		{
			cachedMaterial = P3dShader.BuildMaterial("Hidden/Paint in 3D/Replace");
		}

		public static void Blit(RenderTexture renderTexture, Texture texture, Color tint)
		{
			Instance.SetMaterial(texture, tint);

			Instance.Apply();

			P3dHelper.Blit(renderTexture, Instance.Material);
		}

		public static void BlitFast(RenderTexture renderTexture, Texture texture, Color tint)
		{
			Instance.SetMaterial(texture, tint);

			Instance.Apply();

			Graphics.Blit(default(Texture), renderTexture, Instance.Material);
		}

		public override void Apply()
		{
			Material.SetTexture(P3dShader._Texture, Texture);
			Material.SetColor(P3dShader._Color, Color);
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

			return command;
		}

		public void SetMaterial(Texture texture, Color color)
		{
			Blend    = P3dBlendMode.Replace;
			Material = cachedMaterial;
			Texture  = texture;
			Color    = color;
		}
	}
}