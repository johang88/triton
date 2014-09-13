using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Post.Effects
{
	public class Gamma : BaseEffect
	{
		private Resources.ShaderProgram Shader;
		private GammaShaderParams ShaderParams;

		public Gamma(Backend backend, Common.ResourceManager resourceManager, BatchBuffer quadMesh)
			: base(backend, resourceManager, quadMesh)
		{
			Shader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/gamma");
		}

		public void Render(RenderTarget input, RenderTarget output)
		{
			if (ShaderParams == null)
			{
				ShaderParams = new GammaShaderParams();
				Shader.GetUniformLocations(ShaderParams);
			}

			Backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(Shader.Handle, new int[] { input.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering });
			Backend.BindShaderVariable(ShaderParams.SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
		}

		class GammaShaderParams
		{
			public int SamplerScene = 0;
		}
	}
}
