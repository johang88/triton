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
		public HDRSettings HDRSettings = new HDRSettings();
		public ScreenSpaceReflectionsSettings ScreenSpaceReflectionsSettings = new ScreenSpaceReflectionsSettings();

		// Effects
		private readonly Effects.ScreenSpaceReflections ScreenSpaceReflections;
		private readonly Effects.AdaptLuminance AdaptLuminance;
		private readonly Effects.Bloom Bloom;
		private readonly Effects.LensFlares LensFlares;
		private readonly Effects.Tonemap Tonemap;
		private readonly Effects.Gamma Gamma;
		private readonly Effects.FXAA FXAA;
		private readonly Effects.SMAA SMAA;

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
			ScreenSpaceReflections = new Effects.ScreenSpaceReflections(Backend, ResourceManager, QuadMesh);
			AdaptLuminance = new Effects.AdaptLuminance(Backend, ResourceManager, QuadMesh);
			Bloom = new Effects.Bloom(Backend, ResourceManager, QuadMesh);
			LensFlares = new Effects.LensFlares(Backend, ResourceManager, QuadMesh);
			Tonemap = new Effects.Tonemap(Backend, ResourceManager, QuadMesh);
			Gamma = new Effects.Gamma(Backend, ResourceManager, QuadMesh);
			FXAA = new Effects.FXAA(Backend, ResourceManager, QuadMesh);
			SMAA = new Effects.SMAA(Backend, ResourceManager, QuadMesh);

			// Default settings
			HDRSettings.KeyValue = 0.115f;
			HDRSettings.AdaptationRate = 0.5f;
			HDRSettings.BlurSigma = 3.0f;
			HDRSettings.BloomThreshold = 9.0f;

			ScreenSpaceReflectionsSettings.Enable = false;
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
					SMAA.Render(TemporaryRenderTargets[0], TemporaryRenderTargets[1]);
					SwapRenderTargets();
					break;
				case Post.AntiAliasing.Off:
				default:
					break;
			}
		}

		private void ApplyLumianceBloomAndTonemap(float deltaTime)
		{
			var luminance = AdaptLuminance.Render(HDRSettings, TemporaryRenderTargets[0], deltaTime);
			var bloom = Bloom.Render(HDRSettings, TemporaryRenderTargets[0], luminance);
			var lensFlares = LensFlares.Render(HDRSettings, TemporaryRenderTargets[0], luminance);

			Tonemap.Render(HDRSettings, TemporaryRenderTargets[0], TemporaryRenderTargets[1], bloom, lensFlares, luminance);
			SwapRenderTargets();
		}

		private void ApplyScreenSpaceReflections(Camera camera, RenderTarget gbuffer)
		{
			if (!ScreenSpaceReflectionsSettings.Enable)
				return;

			ScreenSpaceReflections.Render(camera, gbuffer, TemporaryRenderTargets[0], TemporaryRenderTargets[1]);
			SwapRenderTargets();
		}

		public RenderTarget Render(Camera camera, RenderTarget gbuffer, RenderTarget input, float deltaTime)
		{
			// We always start by rendering the input texture to a temporary render target
			Backend.BeginPass(TemporaryRenderTargets[1], Vector4.Zero);

			Sprite.RenderQuad(input.Textures[0], Vector2.Zero);
			Sprite.Render(TemporaryRenderTargets[1].Width, TemporaryRenderTargets[1].Height);

			SwapRenderTargets();

			ApplyScreenSpaceReflections(camera, gbuffer);
			ApplyLumianceBloomAndTonemap(deltaTime);

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
