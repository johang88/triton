using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	class SpriteShader
	{
		public int HandleDiffuse;
		public Resources.ShaderProgram Shader;

		private Backend Backend;
		private bool Initialized = false;

		public SpriteShader(Backend backend, Common.ResourceManager resourceManager)
		{
			Backend = backend;
			Shader = resourceManager.Load<Resources.ShaderProgram>("shaders/sprite");
		}

		public void Bind(int textureHandle, bool alphaBlend = true, bool depthWrite = true, bool enableDepthTest = true, Renderer.BlendingFactorSrc src = Renderer.BlendingFactorSrc.One, Renderer.BlendingFactorDest dst = Renderer.BlendingFactorDest.One)
		{
			if (!Initialized)
			{
				HandleDiffuse = Shader.GetAliasedUniform("DiffuseTexture");
				Initialized = true;
			}

			Backend.BeginInstance(Shader.Handle, new int[] { textureHandle }, alphaBlend, depthWrite, enableDepthTest, src, dst);
			Backend.BindShaderVariable(HandleDiffuse, 0);
		}
	}
}
