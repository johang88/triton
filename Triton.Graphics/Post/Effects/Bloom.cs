using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Post.Effects
{
	public class Bloom : BaseEffect
	{
		private Resources.ShaderProgram HighPassShader;
		private Resources.ShaderProgram BlurHorizontalShader;
		private Resources.ShaderProgram BlurVerticalShader;
		private Resources.ShaderProgram QuadShader;

		private HighPassShaderParams HighPassParams;
		private QuadShaderParams QuadParams;
		private BlurShaderParams BlurHorizontalParams;
		private BlurShaderParams BlurVerticalParams;

		private RenderTarget[] BlurTargets;

		private int BlurSampler;

		public Bloom(Backend backend, Common.ResourceManager resourceManager, BatchBuffer quadMesh)
			: base(backend, resourceManager, quadMesh)
		{
			HighPassShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/highpass");
			BlurHorizontalShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/blur", "BLUR_HORIZONTAL");
			BlurVerticalShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/blur", "BLUR_VERTICAL");
			QuadShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/quad");

			// Setup rendertargets
			var width = backend.Width;
			var height = backend.Height;

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

			BlurSampler = Backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});
		}

		public RenderTarget Render(HDRSettings settings, RenderTarget input, RenderTarget luminance)
		{
			if (HighPassParams == null)
			{
				HighPassParams = new HighPassShaderParams();
				QuadParams = new QuadShaderParams();
				BlurHorizontalParams = new BlurShaderParams();
				BlurVerticalParams = new BlurShaderParams();

				HighPassShader.GetUniformLocations(HighPassParams);
				QuadShader.GetUniformLocations(QuadParams);
				BlurHorizontalShader.GetUniformLocations(BlurHorizontalParams);
				BlurVerticalShader.GetUniformLocations(BlurVerticalParams);
			}

			// High pass
			Backend.BeginPass(BlurTargets[0]);
			Backend.BeginInstance(HighPassShader.Handle, new int[] { input.Textures[0].Handle, luminance.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering });
			Backend.BindShaderVariable(HighPassParams.SamplerScene, 0);
			Backend.BindShaderVariable(HighPassParams.SamplerLuminance, 1);
			Backend.BindShaderVariable(HighPassParams.BloomThreshold, settings.BloomThreshold);
			Backend.BindShaderVariable(HighPassParams.KeyValue, settings.KeyValue);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Downsample 1
			Backend.BeginPass(BlurTargets[1]);
			Backend.BeginInstance(QuadShader.Handle, new int[] { BlurTargets[0].Textures[0].Handle },
				samplers: new int[] { BlurSampler });
			Backend.BindShaderVariable(QuadParams.SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Downsample 2
			Backend.BeginPass(BlurTargets[2]);
			Backend.BeginInstance(QuadShader.Handle, new int[] { BlurTargets[1].Textures[0].Handle },
				samplers: new int[] { BlurSampler });
			Backend.BindShaderVariable(QuadParams.SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Downsample 3
			Backend.BeginPass(BlurTargets[4]);
			Backend.BeginInstance(QuadShader.Handle, new int[] { BlurTargets[2].Textures[0].Handle },
				samplers: new int[] { BlurSampler });
			Backend.BindShaderVariable(QuadParams.SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Blur
			for (var i = 0; i < 1; i++)
			{
				Vector2 blurTextureSize = new Vector2(BlurTargets[3].Width, BlurTargets[3].Height);

				// Blur horizontal
				Backend.BeginPass(BlurTargets[3]);
				Backend.BeginInstance(BlurHorizontalShader.Handle, new int[] { BlurTargets[4].Textures[0].Handle },
					samplers: new int[] { Backend.DefaultSamplerNoFiltering });
				Backend.BindShaderVariable(BlurHorizontalParams.SamplerScene, 0);
				Backend.BindShaderVariable(BlurHorizontalParams.BlurSigma, settings.BlurSigma);
				Backend.BindShaderVariable(BlurHorizontalParams.TextureSize, ref blurTextureSize);

				Backend.DrawMesh(QuadMesh.MeshHandle);
				Backend.EndPass();

				// Blur vertical
				Backend.BeginPass(BlurTargets[4]);
				Backend.BeginInstance(BlurVerticalShader.Handle, new int[] { BlurTargets[3].Textures[0].Handle },
					samplers: new int[] { Backend.DefaultSamplerNoFiltering });
				Backend.BindShaderVariable(BlurVerticalParams.SamplerScene, 0);
				Backend.BindShaderVariable(BlurVerticalParams.BlurSigma, settings.BlurSigma);
				Backend.BindShaderVariable(BlurVerticalParams.TextureSize, ref blurTextureSize);

				Backend.DrawMesh(QuadMesh.MeshHandle);
				Backend.EndPass();
			}

			return BlurTargets[4];
		}

		class HighPassShaderParams
		{
			public int SamplerScene = 0;
			public int SamplerLuminance = 0;
			public int BloomThreshold = 0;
			public int KeyValue = 0;
		}

		class QuadShaderParams
		{
			public int SamplerScene = 0;
		}

		class BlurShaderParams
		{
			public int ModelViewProjection = 0;
			public int SamplerScene = 0;
			public int TextureSize = 0;
			public int BlurSigma = 0;
		}
	}
}
