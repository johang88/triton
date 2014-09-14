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
		private Resources.ShaderProgram Shader;
		private TonemapShaderParams ShaderParams;

		private int BlurSampler;

		public Tonemap(Backend backend, Common.ResourceManager resourceManager, BatchBuffer quadMesh)
			: base(backend, resourceManager, quadMesh)
		{
			Shader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/tonemap");

			BlurSampler = Backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});
		}

		public void Render(HDRSettings settings, RenderTarget input, RenderTarget output, RenderTarget bloom, RenderTarget lensFlares, RenderTarget luminance)
		{
			if (ShaderParams == null)
			{
				ShaderParams = new TonemapShaderParams();
				Shader.GetUniformLocations(ShaderParams);
			}

			Backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(Shader.Handle, new int[] { input.Textures[0].Handle, bloom.Textures[0].Handle, lensFlares.Textures[0].Handle, luminance.Textures[0].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering, BlurSampler, BlurSampler, Backend.DefaultSamplerNoFiltering });
			Backend.BindShaderVariable(ShaderParams.SamplerScene, 0);
			Backend.BindShaderVariable(ShaderParams.SamplerBloom, 1);
			Backend.BindShaderVariable(ShaderParams.SamplerLensFlares, 2);
			Backend.BindShaderVariable(ShaderParams.SamplerLuminance, 3);
			Backend.BindShaderVariable(ShaderParams.KeyValue, settings.KeyValue);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
		}

		class TonemapShaderParams
		{
			public int SamplerScene = 0;
			public int SamplerBloom = 0;
			public int SamplerLensFlares = 0;
			public int SamplerLuminance = 0;
			public int KeyValue = 0;
		}
	}
}
