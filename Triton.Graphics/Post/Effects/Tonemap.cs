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

		private int[] Textures = new int[4];
		private int[] Samplers;

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

			Samplers = new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, BlurSampler, BlurSampler };
		}

		public void Render(HDRSettings settings, RenderTarget input, RenderTarget output, RenderTarget bloom, RenderTarget lensFlares, RenderTarget luminance)
		{
			if (ShaderParams == null)
			{
				ShaderParams = new TonemapShaderParams();
				Shader.GetUniformLocations(ShaderParams);
			}

			Backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

			
			Textures[0] = input.Textures[0].Handle;
			Textures[1] = luminance.Textures[0].Handle;

			var activeTexture = 2;
			if (settings.EnableBloom)
				Textures[activeTexture++] = bloom.Textures[0].Handle;
			if (settings.EnableLensFlares)
				Textures[activeTexture++] = lensFlares.Textures[0].Handle;

			Backend.BeginInstance(Shader.Handle, Textures, samplers: Samplers);
			Backend.BindShaderVariable(ShaderParams.SamplerScene, 0);
			Backend.BindShaderVariable(ShaderParams.SamplerBloom, 2);
			Backend.BindShaderVariable(ShaderParams.SamplerLensFlares, 3);
			Backend.BindShaderVariable(ShaderParams.SamplerLuminance, 1);
			Backend.BindShaderVariable(ShaderParams.KeyValue, settings.KeyValue);
			Backend.BindShaderVariable(ShaderParams.EnableBloom, settings.EnableBloom ? 1 : 0);
			Backend.BindShaderVariable(ShaderParams.EnableLensFlares, settings.EnableLensFlares ? 1 : 0);

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
			public int EnableBloom = 0;
			public int EnableLensFlares = 0;
		}
	}
}
