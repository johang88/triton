using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	public class DeferredRenderer
	{
		private readonly Common.ResourceManager ResourceManager;
		private readonly Backend Backend;

		private GBufferParams GBufferParams = new GBufferParams();
		private AmbientLightParams AmbientLightParams = new AmbientLightParams();
		private PointLightParams PointLightParams = new PointLightParams();
		private DirectionalLightParams DirectionalLightParams = new DirectionalLightParams();
		private SpotLightParams SpotLightParams = new SpotLightParams();
		private SSAOParams SSAOParams = new SSAOParams();
		private BlurParams Blur1Params = new BlurParams();
		private BlurParams Blur2Params = new BlurParams();
		private CombineParams CombineParams = new CombineParams();

		private Vector2 ScreenSize;

		private RenderTarget GBuffer;
		private RenderTarget LightAccumulation;
		private RenderTarget Output;
		private RenderTarget SSAOTarget1;
		private RenderTarget SSAOTarget2;

		private BatchBuffer QuadMesh;
		private Resources.Mesh UnitSphere;

		private Resources.ShaderProgram GBufferShader;
		private Resources.ShaderProgram AmbientLightShader;
		private Resources.ShaderProgram DirectionalLightShader;
		private Resources.ShaderProgram PointLightShader;
		private Resources.ShaderProgram SpotLightShader;
		private Resources.ShaderProgram SSAOShader;
		private Resources.ShaderProgram Blur1Shader;
		private Resources.ShaderProgram Blur2Shader;
		private Resources.ShaderProgram CombineShader;

		private Resources.Texture RandomNoiseTexture;

		private bool HandlesInitialized = false;

		public DeferredRenderer(Common.ResourceManager resourceManager, Backend backend, int width, int height)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (backend == null)
				throw new ArgumentNullException("backend");

			ResourceManager = resourceManager;
			Backend = backend;

			ScreenSize = new Vector2(width, height);

			GBuffer = Backend.CreateRenderTarget("gbuffer", width, height, Triton.Renderer.PixelInternalFormat.Rgba32f, 4, true);
			LightAccumulation = Backend.CreateRenderTarget("light_accumulation", width, height, Triton.Renderer.PixelInternalFormat.Rgba32f, 1, false, GBuffer.Handle);
			Output = Backend.CreateRenderTarget("deferred_output", width, height, Triton.Renderer.PixelInternalFormat.Rgba32f, 1, false, GBuffer.Handle);

			int ssaoScale = 1;
			SSAOTarget1 = Backend.CreateRenderTarget("ssao1", width / ssaoScale, height / ssaoScale, Triton.Renderer.PixelInternalFormat.Rgba32f, 1, false);
			SSAOTarget2 = Backend.CreateRenderTarget("ssao2", width / ssaoScale, height / ssaoScale, Triton.Renderer.PixelInternalFormat.Rgba32f, 1, false);

			GBufferShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/gbuffer");
			AmbientLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/ambient");
			DirectionalLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/directional");
			PointLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/point");
			SpotLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/spot");
			SSAOShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/ssao");
			Blur1Shader = ResourceManager.Load<Resources.ShaderProgram>("shaders/hdr/blur1");
			Blur2Shader = ResourceManager.Load<Resources.ShaderProgram>("shaders/hdr/blur2");
			CombineShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/deferred/combine");

			RandomNoiseTexture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/random_n");

			QuadMesh = Backend.CreateBatchBuffer();
			QuadMesh.Begin();
			QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			QuadMesh.End();

			UnitSphere = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/unit_sphere");
		}

		public void InitializeHandles()
		{
			GBufferShader.GetUniformLocations(GBufferParams);
			CombineShader.GetUniformLocations(CombineParams);
			AmbientLightShader.GetUniformLocations(AmbientLightParams);
			DirectionalLightShader.GetUniformLocations(DirectionalLightParams);
			PointLightShader.GetUniformLocations(PointLightParams);
			SpotLightShader.GetUniformLocations(SpotLightParams);
			SSAOShader.GetUniformLocations(SSAOParams);
			Blur1Shader.GetUniformLocations(Blur1Params);
			Blur2Shader.GetUniformLocations(Blur2Params);
		}

		public RenderTarget Render(Stage stage, Camera camera)
		{
			if (!HandlesInitialized)
			{
				InitializeHandles();
				HandlesInitialized = true;
			}

			// Init common matrices
			Matrix4 view, projection;
			camera.GetViewMatrix(out view);
			camera.GetProjectionMatrix(out projection);

			// Render scene to GBuffer
			Backend.BeginPass(GBuffer, new Vector4(0.0f, 0.0f, 0.0f, 0.0f), true);
			RenderScene(stage, ref view, ref projection);
			Backend.EndPass();

			// SSAO
			RenderSSAO(ref view, ref projection);

			// Render light accumulation
			Backend.BeginPass(LightAccumulation, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), false);

			RenderAmbientLight(stage);
			RenderLights(camera, ref view, ref projection, stage.GetLights());

			Backend.EndPass();

			// Combine final pass
			Backend.BeginPass(Output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), false);

			Backend.BeginInstance(CombineShader.Handle, new int[] { LightAccumulation.Textures[0].Handle, SSAOTarget2.Textures[0].Handle }, true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
			Backend.BindShaderVariable(CombineParams.HandleLightTexture, 0);
			Backend.BindShaderVariable(CombineParams.HandleSSAOTexture, 1);

			Backend.DrawMesh(QuadMesh.MeshHandle);

			Backend.EndPass();

			return Output;
		}

		private void RenderSSAO(ref Matrix4 view, ref Matrix4 projection)
		{
			// SSAO
			Backend.BeginPass(SSAOTarget2, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

			var modelViewProjection = Matrix4.Identity;
			Backend.BeginInstance(SSAOShader.Handle, new int[] { GBuffer.Textures[2].Handle, GBuffer.Textures[1].Handle, RandomNoiseTexture.Handle });
			Backend.BindShaderVariable(SSAOParams.HandleModelViewProjection, ref modelViewProjection);

			Backend.BindShaderVariable(SSAOParams.HandlePositionTexture, 0);
			Backend.BindShaderVariable(SSAOParams.HandleNormalTexture, 1);
			Backend.BindShaderVariable(SSAOParams.HandleRandomTexture, 2);

			var noiseScale = new Vector2(ScreenSize.X / 64, ScreenSize.Y / 64);
			Backend.BindShaderVariable(SSAOParams.HandleNoiseScale, ref noiseScale);

			Backend.DrawMesh(QuadMesh.MeshHandle);

			Backend.EndPass();

			// Blur 1
			Backend.BeginPass(SSAOTarget1, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(Blur1Shader.Handle, new int[] { SSAOTarget2.Textures[0].Handle });
			Backend.BindShaderVariable(Blur1Params.HandleModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(Blur1Params.HandleSceneTexture, 0);
			Backend.BindShaderVariable(Blur1Params.HandleTexelSize, 1.0f / (float)SSAOTarget1.Width);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Blur 2
			Backend.BeginPass(SSAOTarget2, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(Blur2Shader.Handle, new int[] { SSAOTarget1.Textures[0].Handle });
			Backend.BindShaderVariable(Blur2Params.HandleModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(Blur2Params.HandleSceneTexture, 0);
			Backend.BindShaderVariable(Blur2Params.HandleTexelSize, 1.0f / (float)SSAOTarget1.Height);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
		}

		private void RenderScene(Stage stage, ref Matrix4 view, ref Matrix4 projection)
		{
			var modelViewProjection = Matrix4.Identity;

			var meshes = stage.GetMeshes();
			foreach (var mesh in meshes)
			{
				var world = Matrix4.CreateTranslation(mesh.Position) * Matrix4.Rotate(mesh.Orientation);
				modelViewProjection = world * view * projection;

				foreach (var subMesh in mesh.Mesh.SubMeshes)
				{
					var worldView = world * view;
					var itWorldView = Matrix4.Transpose(Matrix4.Invert(worldView));

					subMesh.Material.BindMaterial(Backend, ref world, ref worldView, ref itWorldView, ref modelViewProjection);
					Backend.DrawMesh(subMesh.Handle);
					Backend.EndInstance();
				}
			}
		}

		private void RenderAmbientLight(Stage stage)
		{
			Matrix4 modelViewProjection = Matrix4.Identity;

			var ambientColor = new Vector3((float)System.Math.Pow(stage.AmbientColor.X, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Y, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Z, 2.2f));

			Backend.BeginInstance(AmbientLightShader.Handle, new int[] { GBuffer.Textures[0].Handle }, true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
			Backend.BindShaderVariable(AmbientLightParams.HandleDiffuseTexture, 0);
			Backend.BindShaderVariable(AmbientLightParams.HandleModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(AmbientLightParams.HandleAmbientColor, ref ambientColor);

			Backend.DrawMesh(QuadMesh.MeshHandle);
		}

		private void RenderLights(Camera camera, ref Matrix4 view, ref Matrix4 projection, IReadOnlyCollection<Light> lights)
		{
			Matrix4 modelViewProjection = Matrix4.Identity;
			Vector3 cameraPositionViewSpace;
			Vector3.Transform(ref camera.Position, ref view, out cameraPositionViewSpace);

			foreach (var light in lights)
			{
				if (!light.Enabled)
					continue;

				var radius = light.Range;

				var cullFaceMode = Triton.Renderer.CullFaceMode.Back;
				var depthFunction = Triton.Renderer.DepthFunction.Lequal;

				var delta = light.Position - camera.Position;
				if (delta.Length <= radius)
				{
					cullFaceMode = Triton.Renderer.CullFaceMode.Front;
					depthFunction = Renderer.DepthFunction.Gequal;
				}

				var world = Matrix4.Scale(radius) * Matrix4.CreateTranslation(light.Position);
				modelViewProjection = world * view * projection;
				var lightColor = new Vector3((float)System.Math.Pow(light.Color.X, 2.2f), (float)System.Math.Pow(light.Color.Y, 2.2f), (float)System.Math.Pow(light.Color.Z, 2.2f));

				if (light.Type == LighType.Directional)
				{
					modelViewProjection = Matrix4.Identity;

					Backend.BeginInstance(DirectionalLightShader.Handle, new int[] { GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, GBuffer.Textures[0].Handle }, true, true, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, cullFaceMode);
					Backend.BindShaderVariable(DirectionalLightParams.HandleNormalTexture, 0);
					Backend.BindShaderVariable(DirectionalLightParams.HandlePositionTexture, 1);
					Backend.BindShaderVariable(DirectionalLightParams.HandleSpecularTexture, 2);
					Backend.BindShaderVariable(DirectionalLightParams.HandleDiffuseTexture, 3);
					Backend.BindShaderVariable(DirectionalLightParams.HandleScreenSize, ref ScreenSize);

					var lightDirWS = light.Direction.Normalize();

					var lightDirection = Vector3.Transform(light.Direction, Matrix4.Transpose(Matrix4.Invert(view)));
					lightDirection = lightDirection.Normalize();

					Backend.BindShaderVariable(DirectionalLightParams.HandleModelViewProjection, ref modelViewProjection);
					Backend.BindShaderVariable(DirectionalLightParams.HandleLightDirection, ref lightDirection);
					Backend.BindShaderVariable(DirectionalLightParams.HandleLightColor, ref lightColor);
					Backend.BindShaderVariable(DirectionalLightParams.HandleCameraPosition, ref cameraPositionViewSpace);

					Backend.DrawMesh(QuadMesh.MeshHandle);

					Backend.EndInstance();
				}
				else if (light.Type == LighType.PointLight)
				{
					Backend.BeginInstance(PointLightShader.Handle, new int[] { GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, GBuffer.Textures[0].Handle }, true, false, true, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, cullFaceMode, true, depthFunction);
					Backend.BindShaderVariable(PointLightParams.HandleNormalTexture, 0);
					Backend.BindShaderVariable(PointLightParams.HandlePositionTexture, 1);
					Backend.BindShaderVariable(PointLightParams.HandleSpecularTexture, 2);
					Backend.BindShaderVariable(PointLightParams.HandleDiffuseTexture, 3);
					Backend.BindShaderVariable(PointLightParams.HandleScreenSize, ref ScreenSize);

					Vector3 lightPosition;
					Vector3.Transform(ref light.Position, ref view, out lightPosition);

					Backend.BindShaderVariable(PointLightParams.HandleModelViewProjection, ref modelViewProjection);
					Backend.BindShaderVariable(PointLightParams.HandleLightPosition, ref lightPosition);
					Backend.BindShaderVariable(PointLightParams.HandleLightColor, ref lightColor);
					Backend.BindShaderVariable(PointLightParams.HandleLightRange, light.Range);
					Backend.BindShaderVariable(PointLightParams.HandleCameraPosition, ref cameraPositionViewSpace);

					Backend.DrawMesh(UnitSphere.SubMeshes[0].Handle);

					Backend.EndInstance();
				}
				else if (light.Type == LighType.SpotLight)
				{
					Backend.BeginInstance(SpotLightShader.Handle, new int[] { GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, GBuffer.Textures[0].Handle }, true, false, true, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, cullFaceMode, true, depthFunction);
					Backend.BindShaderVariable(SpotLightParams.HandleNormalTexture, 0);
					Backend.BindShaderVariable(SpotLightParams.HandlePositionTexture, 1);
					Backend.BindShaderVariable(SpotLightParams.HandleSpecularTexture, 2);
					Backend.BindShaderVariable(SpotLightParams.HandleDiffuseTexture, 3); ;
					Backend.BindShaderVariable(SpotLightParams.HandleScreenSize, ref ScreenSize);

					Vector3 lightPosition;
					Vector3.Transform(ref light.Position, ref view, out lightPosition);

					var lightDirWS = light.Direction.Normalize();

					var lightDirection = Vector3.Transform(light.Direction, Matrix4.Transpose(Matrix4.Invert(view)));
					lightDirection = lightDirection.Normalize();

					Backend.BindShaderVariable(SpotLightParams.HandleModelViewProjection, ref modelViewProjection);
					Backend.BindShaderVariable(SpotLightParams.HandleLightPosition, ref lightPosition);
					Backend.BindShaderVariable(SpotLightParams.HandleLightColor, ref lightColor);
					Backend.BindShaderVariable(SpotLightParams.HandleLightRange, light.Range);
					Backend.BindShaderVariable(SpotLightParams.HandleLightDirection, ref lightDirection);

					var spotParams = new Vector2((float)System.Math.Cos(light.InnerAngle / 2.0f), (float)System.Math.Cos(light.OuterAngle / 2.0f));
					Backend.BindShaderVariable(SpotLightParams.HandleSpotLightParams, ref spotParams);
					Backend.BindShaderVariable(SpotLightParams.HandleCameraPosition, ref cameraPositionViewSpace);

					Backend.DrawMesh(UnitSphere.SubMeshes[0].Handle);

					Backend.EndInstance();
				}
			}
		}
	}
}
