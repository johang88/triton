using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.HDR
{
	public class HDRRenderer
	{
		private readonly Common.ResourceManager ResourceManager;
		private readonly Backend Backend;

		private BatchBuffer QuadMesh;

		public RenderTarget Blur1Target;
		public RenderTarget Blur2Target;

		private bool HandlesInitialized = false;

		private Resources.ShaderProgram TonemapShader;
		private Resources.ShaderProgram HighPassShader;
		
		private Resources.ShaderProgram Blur1Shader;
		private Resources.ShaderProgram Blur2Shader;

		private TonemapParams TonemapParams = new TonemapParams();
		private HighPassParams HighPassParams = new HighPassParams();
		
		private BlurParams Blur1Params = new BlurParams();
		private BlurParams Blur2Params = new BlurParams();

		public float Exposure = 1.0f;
		public Vector3 WhitePoint = new Vector3(1, 1, 1) * 1.2f;

		public HDRRenderer(Common.ResourceManager resourceManager, Backend backend, int width, int height)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (backend == null)
				throw new ArgumentNullException("backend");

			ResourceManager = resourceManager;
			Backend = backend;

			int blurScale = 2;
			Blur1Target = Backend.CreateRenderTarget("blur1", width / blurScale, height / blurScale, Triton.Renderer.PixelInternalFormat.Rgba16f, 1, false);
			Blur2Target = Backend.CreateRenderTarget("blur2", width / blurScale, height / blurScale, Triton.Renderer.PixelInternalFormat.Rgba16f, 1, false);

			TonemapShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/hdr/tonemap");
			HighPassShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/hdr/highpass");
			Blur1Shader = ResourceManager.Load<Resources.ShaderProgram>("shaders/hdr/blur1");
			Blur2Shader = ResourceManager.Load<Resources.ShaderProgram>("shaders/hdr/blur2");

			QuadMesh = Backend.CreateBatchBuffer();
			QuadMesh.Begin();
			QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			QuadMesh.End();
		}

		public void InitializeHandles()
		{
			TonemapParams.HandleMVP = TonemapShader.GetAliasedUniform("ModelViewProjection"); ;
			TonemapParams.HandleScene = TonemapShader.GetAliasedUniform("SceneTexture");
			TonemapParams.HandleBloom = TonemapShader.GetAliasedUniform("BloomTexture");
			TonemapParams.HandleWhitePoint = TonemapShader.GetAliasedUniform("WhitePoint");
			TonemapParams.HandleExposure = TonemapShader.GetAliasedUniform("Exposure");

			HighPassParams.HandleMVP = HighPassShader.GetAliasedUniform("ModelViewProjection"); ;
			HighPassParams.HandleScene = HighPassShader.GetAliasedUniform("SceneTexture");
			HighPassParams.HandleWhitePoint = HighPassShader.GetAliasedUniform("WhitePoint");
			HighPassParams.HandleExposure = HighPassShader.GetAliasedUniform("Exposure");

			Blur1Params.HandleMVP = Blur1Shader.GetAliasedUniform("ModelViewProjection"); ;
			Blur1Params.HandleScene = Blur1Shader.GetAliasedUniform("SceneTexture");
			Blur1Params.HandleTexelSize = Blur1Shader.GetAliasedUniform("TexelSize");

			Blur2Params.HandleMVP = Blur2Shader.GetAliasedUniform("ModelViewProjection"); ;
			Blur2Params.HandleScene = Blur2Shader.GetAliasedUniform("SceneTexture");
			Blur2Params.HandleTexelSize = Blur2Shader.GetAliasedUniform("TexelSize");
		}

		public void Render(Camera camera, RenderTarget inputTarget)
		{
			if (!HandlesInitialized)
			{
				InitializeHandles();
				HandlesInitialized = true;
			}

			// High pass
			var modelViewProjection = Matrix4.Identity;

			Backend.BeginPass(Blur2Target, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(HighPassShader.Handle, new int[] { inputTarget.Textures[0].Handle });
			Backend.BindShaderVariable(HighPassParams.HandleMVP, ref modelViewProjection);
			Backend.BindShaderVariable(HighPassParams.HandleScene, 0);
			Backend.BindShaderVariable(HighPassParams.HandleWhitePoint, ref WhitePoint);
			Backend.BindShaderVariable(HighPassParams.HandleExposure, Exposure);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Blur 1
			Backend.BeginPass(Blur1Target, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(Blur1Shader.Handle, new int[] { Blur2Target.Textures[0].Handle });
			Backend.BindShaderVariable(Blur1Params.HandleMVP, ref modelViewProjection);
			Backend.BindShaderVariable(Blur1Params.HandleScene, 0);
			Backend.BindShaderVariable(Blur1Params.HandleTexelSize, 1.0f / (float)Blur1Target.Width);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Blur 2
			Backend.BeginPass(Blur2Target, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(Blur2Shader.Handle, new int[] { Blur1Target.Textures[0].Handle });
			Backend.BindShaderVariable(Blur2Params.HandleMVP, ref modelViewProjection);
			Backend.BindShaderVariable(Blur2Params.HandleScene, 0);
			Backend.BindShaderVariable(Blur2Params.HandleTexelSize, 1.0f / (float)Blur1Target.Height);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Tonemap and apply glow!
			Backend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(TonemapShader.Handle, new int[] { inputTarget.Textures[0].Handle, Blur2Target.Textures[0].Handle });
			Backend.BindShaderVariable(TonemapParams.HandleMVP, ref modelViewProjection);
			Backend.BindShaderVariable(TonemapParams.HandleScene, 0);
			Backend.BindShaderVariable(TonemapParams.HandleBloom, 1);
			Backend.BindShaderVariable(TonemapParams.HandleWhitePoint, ref WhitePoint);
			Backend.BindShaderVariable(TonemapParams.HandleExposure, Exposure);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
		}
	}
}
