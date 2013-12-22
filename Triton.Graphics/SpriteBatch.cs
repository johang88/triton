using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public class SpriteBatch
	{
		private readonly BatchBuffer Buffer;
		private readonly Resources.ShaderProgram Shader;
		private ShaderParams Params;
		private Backend Backend;
		private List<QuadInfo> Quads = new List<QuadInfo>();
		private int LastQuad = 0;

		/// <summary>
		/// Should always be created through Backend.CreateSpriteBatch
		/// </summary>
		internal SpriteBatch(Backend backend, Renderer.RenderSystem renderSystem, Common.ResourceManager resourceManager)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");
			if (renderSystem == null)
				throw new ArgumentNullException("renderSystem");
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");

			Backend = backend;

			Buffer = new BatchBuffer(renderSystem, new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
				{
					new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 3, 0),
					new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.TexCoord, Renderer.VertexPointerType.Float, 2, sizeof(float) * 3),
					new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Color, Renderer.VertexPointerType.Float, 4, sizeof(float) * 5),
				}), 32);

			Shader = resourceManager.Load<Resources.ShaderProgram>("shaders/sprite");

			Quads = new List<QuadInfo>();
			for (var i = 0; i < 32; i++)
			{
				Quads.Add(new QuadInfo());
			}
		}

		public void RenderQuad(Resources.Texture texture, Vector2 position)
		{
			RenderQuad(texture, position, new Vector2(texture.Width, texture.Height), Vector4.One);
		}

		public void RenderQuad(Resources.Texture texture, Vector2 position, Vector4 color)
		{
			RenderQuad(texture, position, new Vector2(texture.Width, texture.Height), color);
		}

		public void RenderQuad(Resources.Texture texture, Vector2 position, Vector2 size)
		{
			RenderQuad(texture, position, size, Vector4.One);
		}

		public void RenderQuad(Resources.Texture texture, Vector2 position, Vector2 size, Vector4 color)
		{
			RenderQuad(texture, position, size, Vector2.Zero, Vector2.One, color);
		}

		public void RenderQuad(Resources.Texture texture, Vector2 position, Vector2 size, Vector2 uvPosition, Vector2 uvSize)
		{
			RenderQuad(texture, position, size, uvPosition, uvSize, Vector4.One);
		}

		public void RenderQuad(Resources.Texture texture, Vector2 position, Vector2 size, Vector2 uvPosition, Vector2 uvSize, Vector4 color)
		{
			if (LastQuad == Quads.Count)
			{
				var quadsToCreate = Quads.Count / 2;
				for (var i = 0; i < quadsToCreate; i++)
				{
					Quads.Add(new QuadInfo());
				}
			}

			var quad = Quads[LastQuad++];
			quad.Init(texture, position, size, uvPosition, uvSize, color);
		}

		public void Render(int width, int height)
		{
			if (Params == null)
			{
				Params = new ShaderParams();
				Shader.GetUniformLocations(Params);
			}

			if (LastQuad == 0) // Bail out
				return;

			Resources.Texture lastTexture = null;

			var projectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, width, 0, height, -1.0f, 1.0f);

			for (var i = 0; i < LastQuad; i++)
			{
				var quad = Quads[i];
				if (lastTexture != quad.Texture)
				{
					if (lastTexture != null)
					{
						Buffer.EndInline(Backend);
						
						Backend.BeginInstance(Shader.Handle, new int[] { lastTexture.Handle }, true, false, false);
						Backend.BindShaderVariable(Params.HandleDiffuseTexture, 0);
						Backend.BindShaderVariable(Params.HandleModelViewProjection, ref projectionMatrix);
						Backend.DrawMesh(Buffer.MeshHandle);
						Backend.EndInstance();
					}

					Buffer.Begin();
					lastTexture = quad.Texture;
				}

				Buffer.AddQuad(quad.Position, quad.Size, quad.UvPositon, quad.UvSize, quad.Color);
			}

			Buffer.EndInline(Backend);

			// Render final batch
			Backend.BeginInstance(Shader.Handle, new int[] { lastTexture.Handle }, true, false, false, Renderer.BlendingFactorSrc.SrcAlpha, Renderer.BlendingFactorDest.OneMinusSrcAlpha);
			Backend.BindShaderVariable(Params.HandleDiffuseTexture, 0);
			Backend.BindShaderVariable(Params.HandleModelViewProjection, ref projectionMatrix);
			Backend.DrawMesh(Buffer.MeshHandle);
			Backend.EndInstance();

			LastQuad = 0;
		}

		class ShaderParams
		{
			public int HandleModelViewProjection = 0;
			public int HandleDiffuseTexture = 0;
		}

		class QuadInfo
		{
			public void Init(Resources.Texture texture, Vector2 position, Vector2 size, Vector2 uvPosition, Vector2 uvSize, Vector4 color)
			{
				Texture = texture;
				Position = position;
				Size = size;
				UvPositon = uvPosition;
				UvSize = uvSize;
				Color = color;
			}

			public Resources.Texture Texture;
			public Vector2 Position;
			public Vector2 Size;
			public Vector2 UvPositon;
			public Vector2 UvSize;
			public Vector4 Color;
		}
	}
}
