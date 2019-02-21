using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Post.Effects
{
	public class Gamma : BaseEffect
	{
		private Resources.ShaderProgram _shader;
		private GammaShaderParams _shaderParams;

        public Resources.Texture ColorCorrectLUT { get; set; }
        public bool EnableColorCorrection { get; set; } = false;

        public Gamma(Backend backend, BatchBuffer quadMesh)
			: base(backend, quadMesh)
		{
		}

		internal override void LoadResources(Triton.Resources.ResourceManager resourceManager)
		{
			base.LoadResources(resourceManager);
			_shader = resourceManager.Load<Resources.ShaderProgram>("/shaders/post/gamma");
		}

		public void Render(RenderTarget input, RenderTarget output)
		{
			if (_shaderParams == null)
			{
				_shaderParams = new GammaShaderParams();
				_shader.BindUniformLocations(_shaderParams);
			}

			_backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			_backend.BeginInstance(_shader.Handle, new int[] { input.Textures[0].Handle, ColorCorrectLUT.Handle },
				samplers: new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering });
			_backend.BindShaderVariable(_shaderParams.SamplerScene, 0);
			_backend.BindShaderVariable(_shaderParams.SamplerColorCorrect, 1);
			_backend.BindShaderVariable(_shaderParams.EnableColorCorrection, EnableColorCorrection ? 1 : 0);

            _backend.DrawMesh(_quadMesh.MeshHandle);
			_backend.EndPass();
		}

		class GammaShaderParams
		{
			public int SamplerScene = 0;
            public int SamplerColorCorrect = 0;
            public int EnableColorCorrection = 0;
        }
	}
}
