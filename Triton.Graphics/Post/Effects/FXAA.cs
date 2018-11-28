using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Post.Effects
{
	public class FXAA : BaseEffect
	{
		private Resources.ShaderProgram _shader;
		private FXAAShaderParams _shaderParams;

		public FXAA(Backend backend, BatchBuffer quadMesh)
			: base(backend, quadMesh)
		{
		}

		internal override void LoadResources(Triton.Resources.ResourceManager resourceManager)
		{
			base.LoadResources(resourceManager);

			_shader = resourceManager.Load<Resources.ShaderProgram>("/shaders/post/fxaa");
		}

		public void Render(RenderTarget input, RenderTarget output)
		{
			if (_shaderParams == null)
			{
				_shaderParams = new FXAAShaderParams();
				_shader.BindUniformLocations(_shaderParams);
			}

			var screenSize = new Vector2(input.Textures[0].Width, input.Textures[0].Height);

			_backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			_backend.BeginInstance(_shader.Handle, new int[] { input.Textures[0].Handle },
				samplers: new int[] { _backend.DefaultSamplerNoFiltering });
			_backend.BindShaderVariable(_shaderParams.SamplerScene, 0);
			_backend.BindShaderVariable(_shaderParams.TextureSize, ref screenSize);

			_backend.DrawMesh(_quadMesh.MeshHandle);
			_backend.EndPass();
		}

		class FXAAShaderParams
		{
			public int SamplerScene = 0;
			public int TextureSize = 0;
		}
	}
}
