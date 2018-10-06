using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;

namespace Triton.Graphics.Post.Effects
{
	public class Tonemap : BaseEffect
	{
		private Resources.ShaderProgram _shader;
		private TonemapShaderParams _shaderParams;

		private readonly int[] _textures = new int[4];
		private readonly int[] _samplers;

		public Tonemap(Backend backend, BatchBuffer quadMesh)
			: base(backend, quadMesh)
		{
			int blurSampler = _backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});

			_samplers = new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSampler, blurSampler, blurSampler };
		}

		internal override void LoadResources(Common.ResourceManager resourceManager)
		{
			base.LoadResources(resourceManager);
			_shader = resourceManager.Load<Resources.ShaderProgram>("/shaders/post/tonemap");
		}

		public void Render(HDRSettings settings, RenderTarget input, RenderTarget output, RenderTarget bloom, RenderTarget lensFlares, RenderTarget luminance)
		{
			if (_shaderParams == null)
			{
				_shaderParams = new TonemapShaderParams();
				_shader.BindUniformLocations(_shaderParams);
			}

			_backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

			_textures[0] = input.Textures[0].Handle;
			_textures[1] = luminance.Textures[0].Handle;

			var activeTexture = 2;
			if (settings.EnableBloom)
				_textures[activeTexture++] = bloom.Textures[0].Handle;

			_backend.BeginInstance(_shader.Handle, _textures, samplers: _samplers);
			_backend.BindShaderVariable(_shaderParams.SamplerScene, 0);
			_backend.BindShaderVariable(_shaderParams.SamplerBloom, 2);
			_backend.BindShaderVariable(_shaderParams.SamplerLensFlares, 3);
			_backend.BindShaderVariable(_shaderParams.SamplerLuminance, 1);
			_backend.BindShaderVariable(_shaderParams.KeyValue, settings.KeyValue);
			_backend.BindShaderVariable(_shaderParams.EnableBloom, settings.EnableBloom ? 1 : 0);
			_backend.BindShaderVariable(_shaderParams.BloomStrength, settings.BloomStrength);
            _backend.BindShaderVariable(_shaderParams.TonemapOperator, (int)settings.TonemapOperator);

			_backend.DrawMesh(_quadMesh.MeshHandle);
			_backend.EndPass();
		}

		class TonemapShaderParams
		{
			public int SamplerScene = 0;
			public int SamplerBloom = 0;
			public int SamplerLensFlares = 0;
			public int SamplerLuminance = 0;
			public int KeyValue = 0;
			public int EnableBloom = 0;
            public int BloomStrength = 0;
            public int EnableLensFlares = 0;
			public int TonemapOperator = 0;
		}
	}
}
