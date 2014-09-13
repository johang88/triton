using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Post.Effects
{
	public class AdaptLuminance : BaseEffect
	{
		private Resources.ShaderProgram LuminanceMapShader;
		private Resources.ShaderProgram AdaptLuminanceShader;

		private LuminanceMapShaderParams LuminanceMapParams;
		private AdaptLuminanceShaderParams AdaptLuminanceParams;

		private RenderTarget LuminanceTarget;
		private RenderTarget[] AdaptLuminanceTargets;

		private int CurrentLuminanceTarget = 0;

		public AdaptLuminance(Backend backend, Common.ResourceManager resourceManager, BatchBuffer quadMesh)
			: base(backend, resourceManager, quadMesh)
		{
			LuminanceMapShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/luminance_map");
			AdaptLuminanceShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/adapt_luminance");

			// Setup render targets
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
		}

		public RenderTarget Render(HDRSettings settings, RenderTarget input, float deltaTime)
		{
			if (LuminanceMapParams == null)
			{
				LuminanceMapParams = new LuminanceMapShaderParams();
				AdaptLuminanceParams = new AdaptLuminanceShaderParams();

				LuminanceMapShader.GetUniformLocations(LuminanceMapShader);
				AdaptLuminanceShader.GetUniformLocations(AdaptLuminanceParams);
			}

			// Calculate luminance
			Backend.BeginPass(LuminanceTarget);
			Backend.BeginInstance(LuminanceMapShader.Handle, new int[] { input.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering });
			Backend.BindShaderVariable(LuminanceMapParams.SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
			Backend.GenerateMips(LuminanceTarget.Textures[0].Handle);

			// Adapt luminace
			var adaptedLuminanceTarget = AdaptLuminanceTargets[CurrentLuminanceTarget];
			var adaptedLuminanceSource = AdaptLuminanceTargets[CurrentLuminanceTarget == 0 ? 1 : 0];
			CurrentLuminanceTarget = (CurrentLuminanceTarget + 1) % 2;

			Backend.BeginPass(adaptedLuminanceTarget);
			Backend.BeginInstance(AdaptLuminanceShader.Handle, new int[] { adaptedLuminanceSource.Textures[0].Handle, LuminanceTarget.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerMipMapNearest });
			Backend.BindShaderVariable(AdaptLuminanceParams.SamplerLastLuminacne, 0);
			Backend.BindShaderVariable(AdaptLuminanceParams.SamplerCurrentLuminance, 1);
			Backend.BindShaderVariable(AdaptLuminanceParams.TimeDelta, deltaTime);
			Backend.BindShaderVariable(AdaptLuminanceParams.Tau, settings.AdaptationRate);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			return adaptedLuminanceTarget;
		}

		class LuminanceMapShaderParams
		{
			public int SamplerScene = 0;
		}

		class AdaptLuminanceShaderParams
		{
			public int SamplerLastLuminacne = 0;
			public int SamplerCurrentLuminance = 0;
			public int TimeDelta = 0;
			public int Tau = 0;
		}
	}
}
