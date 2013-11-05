using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Math;

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

		private Resources.ShaderProgram BlurShader;

		private TonemapParams TonemapParams = new TonemapParams();
		private HighPassParams HighPassParams = new HighPassParams();

		private BlurParams BlurParams = new BlurParams();

		public float Exposure = 1.0f;
		public Vector3 WhitePoint = new Vector3(1, 1, 1) * 1.2f;

		private Vector4[] BlurWeights = new Vector4[15];
		private Vector4[] BlurOffsetsHorz = new Vector4[15];
		private Vector4[] BlurOffsetsVert = new Vector4[15];

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
			BlurShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/blur");

			QuadMesh = Backend.CreateBatchBuffer();
			QuadMesh.Begin();
			QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			QuadMesh.End();

			BlurHelper.Init(ref BlurWeights, ref BlurOffsetsHorz, ref BlurOffsetsVert, new Vector2(1.0f / (float)Blur1Target.Width, 1.0f / (float)Blur1Target.Height));
		}

		public void InitializeHandles()
		{
			TonemapParams.HandleMVP = TonemapShader.GetAliasedUniform("ModelViewProjection");
			TonemapParams.HandleScene = TonemapShader.GetAliasedUniform("SceneTexture");
			TonemapParams.HandleBloom = TonemapShader.GetAliasedUniform("BloomTexture");
			TonemapParams.HandleWhitePoint = TonemapShader.GetAliasedUniform("WhitePoint");
			TonemapParams.HandleExposure = TonemapShader.GetAliasedUniform("Exposure");

			HighPassParams.HandleMVP = HighPassShader.GetAliasedUniform("ModelViewProjection");
			HighPassParams.HandleScene = HighPassShader.GetAliasedUniform("SceneTexture");
			HighPassParams.HandleWhitePoint = HighPassShader.GetAliasedUniform("WhitePoint");
			HighPassParams.HandleExposure = HighPassShader.GetAliasedUniform("Exposure");

			BlurShader.GetUniformLocations(BlurParams);
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

			for (var i = 0; i < 2; i++)
			{
				// Blur 1
				Backend.BeginPass(Blur1Target, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				Backend.BeginInstance(BlurShader.Handle, new int[] { Blur2Target.Textures[0].Handle });
				Backend.BindShaderVariable(BlurParams.HandleModelViewProjection, ref modelViewProjection);
				Backend.BindShaderVariable(BlurParams.HandleSceneTexture, 0);
				Backend.BindShaderVariable(BlurParams.SampleWeights, ref BlurWeights);
				Backend.BindShaderVariable(BlurParams.HandleSampleOffsets, ref BlurOffsetsHorz);

				Backend.DrawMesh(QuadMesh.MeshHandle);
				Backend.EndPass();

				// Blur 2
				Backend.BeginPass(Blur2Target, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				Backend.BeginInstance(BlurShader.Handle, new int[] { Blur1Target.Textures[0].Handle });
				Backend.BindShaderVariable(BlurParams.HandleModelViewProjection, ref modelViewProjection);
				Backend.BindShaderVariable(BlurParams.HandleSceneTexture, 0);
				Backend.BindShaderVariable(BlurParams.SampleWeights, ref BlurWeights);
				Backend.BindShaderVariable(BlurParams.HandleSampleOffsets, ref BlurOffsetsVert);

				Backend.DrawMesh(QuadMesh.MeshHandle);
				Backend.EndPass();
			}

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
