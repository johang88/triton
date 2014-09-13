using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Post
{
	public class PostEffectManager
	{
		private readonly RenderTarget[] TemporaryRenderTargets = new RenderTarget[2];
		private readonly RenderTargetManager RenderTargetManager;
		private readonly Backend Backend;
		private readonly Common.ResourceManager ResourceManager;
		
		private BatchBuffer QuadMesh;
		private SpriteBatch Sprite;

		// Settings
		public AntiAliasing AntiAliasing = AntiAliasing.FXAA;

		// Effects
		private readonly Effects.Gamma Gamma;
		private readonly Effects.FXAA FXAA;
		
		public PostEffectManager(Common.ResourceManager resourceManager, Backend backend, int width, int height)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (backend == null)
				throw new ArgumentNullException("backend");

			ResourceManager = resourceManager;
			Backend = backend;

			RenderTargetManager = new Post.RenderTargetManager(Backend);

			TemporaryRenderTargets[0] = Backend.CreateRenderTarget("post_0", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0)
			}));

			TemporaryRenderTargets[1] = Backend.CreateRenderTarget("post_1", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0)
			}));

			QuadMesh = Backend.CreateBatchBuffer();
			QuadMesh.Begin();
			QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			QuadMesh.End();

			Sprite = Backend.CreateSpriteBatch();

			// Setup effects
			Gamma = new Effects.Gamma(Backend, ResourceManager, QuadMesh);
			FXAA = new Effects.FXAA(Backend, ResourceManager, QuadMesh);
		}

		void SwapRenderTargets()
		{
			var tmp = TemporaryRenderTargets[0];
			TemporaryRenderTargets[0] = TemporaryRenderTargets[1];
			TemporaryRenderTargets[1] = tmp;
		}

		private void ApplyAA()
		{
			switch (AntiAliasing)
			{
				case AntiAliasing.FXAA:
					FXAA.Render(TemporaryRenderTargets[0], TemporaryRenderTargets[1]);
					SwapRenderTargets();
					break;
				case AntiAliasing.SMAA:
					break;
				case Post.AntiAliasing.Off:
				default:
					break;
			}
		}

		public RenderTarget Render(Camera camera, RenderTarget gbuffer, RenderTarget input, float deltaTime)
		{
			// We always start by rendering the input texture to a temporary render target
			Backend.BeginPass(TemporaryRenderTargets[1], Vector4.Zero);

			Sprite.RenderQuad(input.Textures[0], Vector2.Zero);
			Sprite.Render(TemporaryRenderTargets[1].Width, TemporaryRenderTargets[1].Height);

			SwapRenderTargets();

			ApplyAA();

			// linear -> to gamma space
			Gamma.Render(TemporaryRenderTargets[0], TemporaryRenderTargets[1]);
			SwapRenderTargets();

			return TemporaryRenderTargets[0];
		}

		public void Resize(int newWidth, int newHeight)
		{
			// TODO ...
		}
	}
}
