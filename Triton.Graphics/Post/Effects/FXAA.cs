using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Post.Effects
{
	public class FXAA : BaseEffect
	{
		private Resources.ShaderProgram Shader;
		private FXAAShaderParams ShaderParams;

		public FXAA(Backend backend, Common.ResourceManager resourceManager, BatchBuffer quadMesh)
			: base(backend, resourceManager, quadMesh)
		{
			Shader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/fxaa");
		}

		public void Render(RenderTarget input, RenderTarget output)
		{
			if (ShaderParams == null)
			{
				ShaderParams = new FXAAShaderParams();
				Shader.GetUniformLocations(ShaderParams);
			}

			var screenSize = new Vector2(input.Textures[0].Width, input.Textures[0].Height);

			Backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(Shader.Handle, new int[] { input.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering });
			Backend.BindShaderVariable(ShaderParams.SamplerScene, 0);
			Backend.BindShaderVariable(ShaderParams.TextureSize, ref screenSize);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
		}

		class FXAAShaderParams
		{
			public int SamplerScene = 0;
			public int TextureSize = 0;
		}
	}
}
