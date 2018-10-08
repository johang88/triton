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
		private Resources.ShaderProgram _luminanceMapShader;
		private Resources.ShaderProgram _adaptLuminanceShader;

		private LuminanceMapShaderParams _luminanceMapParams;
		private AdaptLuminanceShaderParams _adaptLuminanceParams;

		private readonly RenderTarget _luminanceTarget;
		private readonly RenderTarget[] _adaptLuminanceTargets;

		private int _currentLuminanceTarget = 0;

		public AdaptLuminance(Backend backend, BatchBuffer quadMesh)
			: base(backend, quadMesh)
		{
			// Setup render targets
			_luminanceTarget = _backend.CreateRenderTarget("avg_luminance", new Definition(1024, 1024, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.R32f, Renderer.PixelType.Float, 0, true),
			}));

			_adaptLuminanceTargets = new RenderTarget[]
			{
				_backend.CreateRenderTarget("adapted_luminance_0", new Definition(1, 1, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.R32f, Renderer.PixelType.Float, 0),
				})),
				_backend.CreateRenderTarget("adapted_luminance_1", new Definition(1, 1, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.R32f, Renderer.PixelType.Float, 0),
				}))
			};
		}

		internal override void LoadResources(Common.ResourceManager resourceManager)
		{
			base.LoadResources(resourceManager);

			_luminanceMapShader = resourceManager.Load<Resources.ShaderProgram>("/shaders/post/luminance_map");
			_adaptLuminanceShader = resourceManager.Load<Resources.ShaderProgram>("/shaders/post/adapt_luminance");
		}

		public RenderTarget Render(HDRSettings settings, RenderTarget input, float deltaTime)
		{
			if (_luminanceMapParams == null)
			{
				_luminanceMapParams = new LuminanceMapShaderParams();
				_adaptLuminanceParams = new AdaptLuminanceShaderParams();

				_luminanceMapShader.BindUniformLocations(_luminanceMapShader);
				_adaptLuminanceShader.BindUniformLocations(_adaptLuminanceParams);
			}

			// Calculate luminance
			_backend.BeginPass(_luminanceTarget);
			_backend.BeginInstance(_luminanceMapShader.Handle, new int[] { input.Textures[0].Handle },
				samplers: new int[] { _backend.DefaultSamplerNoFiltering });
			_backend.BindShaderVariable(_luminanceMapParams.SamplerScene, 0);

			_backend.DrawMesh(_quadMesh.MeshHandle);
			_backend.EndPass();

			_backend.GenerateMips(_luminanceTarget.Textures[0].Handle);

			// Adapt luminace
			var adaptedLuminanceTarget = _adaptLuminanceTargets[_currentLuminanceTarget];
			var adaptedLuminanceSource = _adaptLuminanceTargets[_currentLuminanceTarget == 0 ? 1 : 0];
			_currentLuminanceTarget = (_currentLuminanceTarget + 1) % 2;

			_backend.BeginPass(adaptedLuminanceTarget);
			_backend.BeginInstance(_adaptLuminanceShader.Handle, new int[] { adaptedLuminanceSource.Textures[0].Handle, _luminanceTarget.Textures[0].Handle },
				samplers: new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerMipMapNearest });
			_backend.BindShaderVariable(_adaptLuminanceParams.SamplerLastLuminacne, 0);
			_backend.BindShaderVariable(_adaptLuminanceParams.SamplerCurrentLuminance, 1);
			_backend.BindShaderVariable(_adaptLuminanceParams.TimeDelta, deltaTime);
			_backend.BindShaderVariable(_adaptLuminanceParams.Tau, settings.AdaptationRate);

			_backend.DrawMesh(_quadMesh.MeshHandle);
			_backend.EndPass();

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
