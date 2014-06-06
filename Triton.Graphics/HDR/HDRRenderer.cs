using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Math;
using Triton.Renderer;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.HDR
{
	public class HDRRenderer
	{
		private readonly Common.ResourceManager ResourceManager;
		private readonly Backend Backend;

		private BatchBuffer QuadMesh;

		public RenderTarget[] BlurTargets;
		public RenderTarget LuminanceTarget;
		public RenderTarget[] AdaptLuminanceTargets;

		private bool HandlesInitialized = false;

		private Resources.ShaderProgram TonemapShader;
		private Resources.ShaderProgram HighPassShader;
		private Resources.ShaderProgram BlurHorizontalShader;
		private Resources.ShaderProgram BlurVerticalShader;
		private Resources.ShaderProgram LuminanceMapShader;
		private Resources.ShaderProgram AdaptLuminanceShader;
		private Resources.ShaderProgram QuadShader;

		private int CurrentLuminanceTarget = 0;

		private TonemapParams TonemapParams = new TonemapParams();
		private HighPassParams HighPassParams = new HighPassParams();
		private BlurParams BlurHorizontalParams = new BlurParams();
		private BlurParams BlurVerticalParams = new BlurParams();
		private LuminanceMapParams LuminanceMapParams = new LuminanceMapParams();
		private AdaptLuminanceParams AdaptLuminanceParams = new AdaptLuminanceParams();
		private QuadParams QuadParams = new QuadParams();

		public float BloomThreshold = 9.2f;
		public float BlurSigma = 3.0f;
		public float AdaptationRate = 0.5f;
		public float KeyValue = 0.115f;

		private int BlurSampler;

		public HDRRenderer(Common.ResourceManager resourceManager, Backend backend, int width, int height)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (backend == null)
				throw new ArgumentNullException("backend");

			ResourceManager = resourceManager;
			Backend = backend;

			BlurTargets = new RenderTarget[]
			{
				Backend.CreateRenderTarget("blur_down_src", new Definition(width, height, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
				})),
				Backend.CreateRenderTarget("blur_down_2", new Definition(width / 2, height / 2, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
				})),
				Backend.CreateRenderTarget("blur_down_4", new Definition(width / 4, height / 4, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
				})),
				Backend.CreateRenderTarget("blur_down8_1", new Definition(width / 8, height / 8, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
				})),
				Backend.CreateRenderTarget("blur_down8_2", new Definition(width / 8, height / 8, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
				})),
			};
			LuminanceTarget = Backend.CreateRenderTarget("avg_luminance", new Definition(1024, 1024, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.R32f, Renderer.PixelType.Float, 0, true),
			}));

			AdaptLuminanceTargets = new RenderTarget[]
			{
				Backend.CreateRenderTarget("adapted_luminance_0", new Definition(1, 1, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.R16f, Renderer.PixelType.Float, 0),
				})),
				Backend.CreateRenderTarget("adapted_luminance_1", new Definition(1, 1, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.R16f, Renderer.PixelType.Float, 0),
				}))
			};

			TonemapShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/tonemap");
			HighPassShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/highpass");
			BlurHorizontalShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/blur", "BLUR_HORIZONTAL");
			BlurVerticalShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/blur", "BLUR_VERTICAL");
			LuminanceMapShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/luminance_map");
			AdaptLuminanceShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/adapt_luminance");
			QuadShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/quad");

			QuadMesh = Backend.CreateBatchBuffer();
			QuadMesh.Begin();
			QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			QuadMesh.End();

			BlurSampler = Backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});
		}

		public void InitializeHandles()
		{
			TonemapShader.GetUniformLocations(TonemapParams);
			HighPassShader.GetUniformLocations(HighPassParams);
			BlurHorizontalShader.GetUniformLocations(BlurHorizontalParams);
			BlurVerticalShader.GetUniformLocations(BlurVerticalParams);
			LuminanceMapShader.GetUniformLocations(LuminanceMapParams);
			AdaptLuminanceShader.GetUniformLocations(AdaptLuminanceParams);
			QuadShader.GetUniformLocations(QuadParams);
		}

		public void Render(Camera camera, RenderTarget inputTarget, float deltaTime)
		{
			if (!HandlesInitialized)
			{
				InitializeHandles();
				HandlesInitialized = true;
			}

			var modelViewProjection = Matrix4.Identity;

			// Calculate luminance
			Backend.BeginPass(LuminanceTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(LuminanceMapShader.Handle, new int[] { inputTarget.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering });
			Backend.BindShaderVariable(LuminanceMapParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(LuminanceMapParams.SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
			Backend.GenerateMips(LuminanceTarget.Textures[0].Handle);

			// Adapt luminace
			var adaptedLuminanceTarget = AdaptLuminanceTargets[CurrentLuminanceTarget];
			var adaptedLuminanceSource = AdaptLuminanceTargets[CurrentLuminanceTarget == 0 ? 1 : 0];
			CurrentLuminanceTarget = (CurrentLuminanceTarget + 1) % 2;

			Backend.BeginPass(adaptedLuminanceTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(AdaptLuminanceShader.Handle, new int[] { adaptedLuminanceSource.Textures[0].Handle, LuminanceTarget.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerMipMapNearest });
			Backend.BindShaderVariable(AdaptLuminanceParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(AdaptLuminanceParams.SamplerLastLuminacne, 0);
			Backend.BindShaderVariable(AdaptLuminanceParams.SamplerCurrentLuminance, 1);
			Backend.BindShaderVariable(AdaptLuminanceParams.TimeDelta, deltaTime);
			Backend.BindShaderVariable(AdaptLuminanceParams.Tau, AdaptationRate);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// High pass
			Backend.BeginPass(BlurTargets[0], new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(HighPassShader.Handle, new int[] { inputTarget.Textures[0].Handle, adaptedLuminanceSource.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering });
			Backend.BindShaderVariable(HighPassParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(HighPassParams.SamplerScene, 0);
			Backend.BindShaderVariable(HighPassParams.SamplerLuminance, 1);
			Backend.BindShaderVariable(HighPassParams.BloomThreshold, BloomThreshold);
			Backend.BindShaderVariable(HighPassParams.KeyValue, KeyValue);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Downsample 1
			Backend.BeginPass(BlurTargets[1], new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(QuadShader.Handle, new int[] { BlurTargets[0].Textures[0].Handle },
				samplers: new int[] { BlurSampler });
			Backend.BindShaderVariable(QuadParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(QuadParams.SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Downsample 2
			Backend.BeginPass(BlurTargets[2], new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(QuadShader.Handle, new int[] { BlurTargets[1].Textures[0].Handle },
				samplers: new int[] { BlurSampler });
			Backend.BindShaderVariable(QuadParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(QuadParams.SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Downsample 3
			Backend.BeginPass(BlurTargets[4], new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(QuadShader.Handle, new int[] { BlurTargets[2].Textures[0].Handle },
				samplers: new int[] { BlurSampler });
			Backend.BindShaderVariable(QuadParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(QuadParams.SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Blur
			for (var i = 0; i < 1; i++)
			{
				Vector2 blurTextureSize = new Vector2(BlurTargets[3].Width, BlurTargets[3].Height);

				// Blur horizontal
				Backend.BeginPass(BlurTargets[3], new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				Backend.BeginInstance(BlurHorizontalShader.Handle, new int[] { BlurTargets[4].Textures[0].Handle },
					samplers: new int[] { Backend.DefaultSamplerNoFiltering });
				Backend.BindShaderVariable(BlurHorizontalParams.ModelViewProjection, ref modelViewProjection);
				Backend.BindShaderVariable(BlurHorizontalParams.SamplerScene, 0);
				Backend.BindShaderVariable(BlurHorizontalParams.BlurSigma, BlurSigma);
				Backend.BindShaderVariable(BlurHorizontalParams.TextureSize, ref blurTextureSize);

				Backend.DrawMesh(QuadMesh.MeshHandle);
				Backend.EndPass();

				// Blur vertical
				Backend.BeginPass(BlurTargets[4], new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				Backend.BeginInstance(BlurVerticalShader.Handle, new int[] { BlurTargets[3].Textures[0].Handle },
					samplers: new int[] { Backend.DefaultSamplerNoFiltering });
				Backend.BindShaderVariable(BlurVerticalParams.ModelViewProjection, ref modelViewProjection);
				Backend.BindShaderVariable(BlurVerticalParams.SamplerScene, 0);
				Backend.BindShaderVariable(BlurVerticalParams.BlurSigma, BlurSigma);
				Backend.BindShaderVariable(BlurVerticalParams.TextureSize, ref blurTextureSize);

				Backend.DrawMesh(QuadMesh.MeshHandle);
				Backend.EndPass();
			}

			// Tonemap and apply glow!
			Backend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(TonemapShader.Handle, new int[] { inputTarget.Textures[0].Handle, BlurTargets[4].Textures[0].Handle, adaptedLuminanceSource.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering, BlurSampler, Backend.DefaultSamplerNoFiltering });
			Backend.BindShaderVariable(TonemapParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(TonemapParams.SamplerScene, 0);
			Backend.BindShaderVariable(TonemapParams.SamplerBloom, 1);
			Backend.BindShaderVariable(TonemapParams.SamplerLuminance, 2);
			Backend.BindShaderVariable(TonemapParams.KeyValue, KeyValue);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
		}
	}
}
