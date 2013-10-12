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
		private SpotLightParams SpotLightParams = new SpotLightParams();
		private CombineParams CombineParams = new CombineParams();

		private Vector2 ScreenSize;

		private RenderTarget GBuffer;
		private RenderTarget LightAccumulation;

		private BatchBuffer QuadMesh;
		private Resources.Mesh UnitSphere;

		private Resources.ShaderProgram GBufferShader;
		private Resources.ShaderProgram AmbientLightShader;
		private Resources.ShaderProgram PointLightShader;
		private Resources.ShaderProgram SpotLightShader;
		private Resources.ShaderProgram CombineShader;

		private bool HandlesInitialized = false;

		public DeferredRenderer(Common.ResourceManager resourceManager, Backend backend, int Width, int Height)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (backend == null)
				throw new ArgumentNullException("backend");

			ResourceManager = resourceManager;
			Backend = backend;

			ScreenSize = new Vector2(Width, Height);

			GBuffer = Backend.CreateRenderTarget("full_scene", Width, Height, Triton.Renderer.PixelInternalFormat.Rgba32f, 4, true);
			LightAccumulation = Backend.CreateRenderTarget("light_accumulation", Width, Height, Triton.Renderer.PixelInternalFormat.Rgba32f, 2, true);

			GBufferShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/gbuffer");
			AmbientLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/ambient");
			PointLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/point");
			SpotLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/spot");
			CombineShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/combine");

			QuadMesh = Backend.CreateBatchBuffer();
			QuadMesh.Begin();
			QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			QuadMesh.End();

			UnitSphere = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/unit_sphere");
		}

		public void InitializeHandles()
		{
			GBufferParams.HandleMVP = GBufferShader.GetAliasedUniform("ModelViewProjection");
			GBufferParams.HandleWorld = GBufferShader.GetAliasedUniform("World");
			GBufferParams.HandleDiffuseTexture = GBufferShader.GetAliasedUniform("DiffuseTexture");
			GBufferParams.HandleNormalMap = GBufferShader.GetAliasedUniform("NormalMap");
			GBufferParams.HandleSpecularMap = GBufferShader.GetAliasedUniform("SpecularMap");

			AmbientLightParams.HandleMVP = AmbientLightShader.GetAliasedUniform("ModelViewProjection"); ;
			AmbientLightParams.HandleNormal = AmbientLightShader.GetAliasedUniform("NormalTexture");
			AmbientLightParams.HandleAmbientColor = AmbientLightShader.GetAliasedUniform("AmbientColor");

			PointLightParams.HandleMVP = PointLightShader.GetAliasedUniform("ModelViewProjection"); ;
			PointLightParams.HandleNormal = PointLightShader.GetAliasedUniform("NormalTexture");
			PointLightParams.HandlePosition = PointLightShader.GetAliasedUniform("PositionTexture");
			PointLightParams.HandleLightPositon = PointLightShader.GetAliasedUniform("LightPosition");
			PointLightParams.HandleCameraPosition = PointLightShader.GetAliasedUniform("CameraPosition");
			PointLightParams.HandleLightColor = PointLightShader.GetAliasedUniform("LightColor");
			PointLightParams.HandleLightRange = PointLightShader.GetAliasedUniform("LightRange");
			PointLightParams.HandleScreenSize = PointLightShader.GetAliasedUniform("ScreenSize");
			PointLightParams.HandleSpecular = PointLightShader.GetAliasedUniform("SpecularTexture");

			SpotLightParams.HandleMVP = SpotLightShader.GetAliasedUniform("ModelViewProjection"); ;
			SpotLightParams.HandleNormal = SpotLightShader.GetAliasedUniform("NormalTexture");
			SpotLightParams.HandlePosition = SpotLightShader.GetAliasedUniform("PositionTexture");
			SpotLightParams.HandleLightPositon = SpotLightShader.GetAliasedUniform("LightPosition");
			SpotLightParams.HandleCameraPosition = SpotLightShader.GetAliasedUniform("CameraPosition");
			SpotLightParams.HandleLightColor = SpotLightShader.GetAliasedUniform("LightColor");
			SpotLightParams.HandleLightRange = SpotLightShader.GetAliasedUniform("LightRange");
			SpotLightParams.HandleScreenSize = SpotLightShader.GetAliasedUniform("ScreenSize");
			SpotLightParams.HandleSpotLightParams = SpotLightShader.GetAliasedUniform("SpotLightParams");
			SpotLightParams.HandleDirection = SpotLightShader.GetAliasedUniform("LightDirection");
			SpotLightParams.HandleSpecular = SpotLightShader.GetAliasedUniform("SpecularTexture");

			CombineParams.HandleMVP = CombineShader.GetAliasedUniform("ModelViewProjection"); ;
			CombineParams.HandleDiffuse = CombineShader.GetAliasedUniform("DiffuseTexture");
			CombineParams.HandleLight = CombineShader.GetAliasedUniform("LightTexture");
			CombineParams.HandleSpecular = CombineShader.GetAliasedUniform("SpecularTexture");
		}

		public void Render(Stage stage, Camera camera)
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
			Backend.BeginPass(GBuffer, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
			RenderScene(stage, ref view, ref projection);
			Backend.EndPass();

			// Render light accumulation
			Backend.BeginPass(LightAccumulation, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

			RenderAmbientLight(stage);
			RenderLights(camera, ref view, ref projection, stage.GetLights());

			Backend.EndPass();

			// Combine light and diffuse color
			var modelViewProjection = Matrix4.Identity;

			Backend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(CombineShader.Handle, new int[] { GBuffer.Textures[0].Handle, LightAccumulation.Textures[0].Handle, LightAccumulation.Textures[1].Handle });
			Backend.BindShaderVariable(CombineParams.HandleMVP, ref modelViewProjection);
			Backend.BindShaderVariable(CombineParams.HandleDiffuse, 0);
			Backend.BindShaderVariable(CombineParams.HandleLight, 1);
			Backend.BindShaderVariable(CombineParams.HandleSpecular, 2);

			Backend.DrawMesh(QuadMesh.Mesh.Handles[0]);
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

				var material = mesh.Material;

				Backend.BeginInstance(GBufferShader.Handle, new int[] { material.Diffuse.Handle, material.Normal.Handle, material.Specular.Handle });

				Backend.BindShaderVariable(GBufferParams.HandleMVP, ref modelViewProjection);
				Backend.BindShaderVariable(GBufferParams.HandleWorld, ref world);
				Backend.BindShaderVariable(GBufferParams.HandleDiffuseTexture, 0);
				Backend.BindShaderVariable(GBufferParams.HandleNormalMap, 1);
				Backend.BindShaderVariable(GBufferParams.HandleSpecularMap, 2);

				foreach (var handle in mesh.Mesh.Handles)
				{
					Backend.DrawMesh(handle);
				}

				Backend.EndInstance();
			}
		}

		private void RenderAmbientLight(Stage stage)
		{
			Matrix4 modelViewProjection = Matrix4.Identity;

			Backend.BeginInstance(AmbientLightShader.Handle, new int[] { GBuffer.Textures[1].Handle }, true, true, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
			Backend.BindShaderVariable(AmbientLightParams.HandleNormal, 0);
			Backend.BindShaderVariable(AmbientLightParams.HandleMVP, ref modelViewProjection);
			Backend.BindShaderVariable(AmbientLightParams.HandleAmbientColor, ref stage.AmbientColor);

			Backend.DrawMesh(QuadMesh.Mesh.Handles[0]);
		}

		private void RenderLights(Camera camera, ref Matrix4 view, ref Matrix4 projection, IReadOnlyCollection<Light> lights)
		{
			Matrix4 modelViewProjection = Matrix4.Identity;

			foreach (var light in lights)
			{
				var radius = light.Range;

				var cullFaceMode = Triton.Renderer.CullFaceMode.Back;
				var delta = light.Position - camera.Position;
				if (Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z) <= radius * radius)
				{
					cullFaceMode = Triton.Renderer.CullFaceMode.Front;
				}

				var world = Matrix4.Scale(radius) * Matrix4.CreateTranslation(light.Position);
				modelViewProjection = world * view * projection;

				if (light.Type == LighType.PointLight)
				{
					Backend.BeginInstance(PointLightShader.Handle, new int[] { GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle }, true, true, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, cullFaceMode);
					Backend.BindShaderVariable(PointLightParams.HandleNormal, 0);
					Backend.BindShaderVariable(PointLightParams.HandlePosition, 1);
					Backend.BindShaderVariable(PointLightParams.HandleSpecular, 2);
					Backend.BindShaderVariable(PointLightParams.HandleScreenSize, ref ScreenSize);

					Backend.BindShaderVariable(PointLightParams.HandleMVP, ref modelViewProjection);
					Backend.BindShaderVariable(PointLightParams.HandleLightPositon, ref light.Position);
					Backend.BindShaderVariable(PointLightParams.HandleLightColor, ref light.Color);
					Backend.BindShaderVariable(PointLightParams.HandleLightRange, light.Range);
					Backend.BindShaderVariable(PointLightParams.HandleCameraPosition, ref camera.Position);

					Backend.DrawMesh(UnitSphere.Handles[0]);

					Backend.EndInstance();
				}
				else if (light.Type == LighType.SpotLight)
				{
					Backend.BeginInstance(SpotLightShader.Handle, new int[] { GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle }, true, true, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, cullFaceMode);
					Backend.BindShaderVariable(SpotLightParams.HandleNormal, 0);
					Backend.BindShaderVariable(SpotLightParams.HandlePosition, 1);
					Backend.BindShaderVariable(SpotLightParams.HandleSpecular, 2);
					Backend.BindShaderVariable(SpotLightParams.HandleScreenSize, ref ScreenSize);

					Backend.BindShaderVariable(SpotLightParams.HandleMVP, ref modelViewProjection);
					Backend.BindShaderVariable(SpotLightParams.HandleLightPositon, ref light.Position);
					Backend.BindShaderVariable(SpotLightParams.HandleLightColor, ref light.Color);
					Backend.BindShaderVariable(SpotLightParams.HandleLightRange, light.Range);
					Backend.BindShaderVariable(SpotLightParams.HandleDirection, ref light.Direction);

					var spotParams = new Vector2((float)Math.Cos(light.InnerAngle / 2.0f), (float)Math.Cos(light.OuterAngle / 2.0f));
					Backend.BindShaderVariable(SpotLightParams.HandleSpotLightParams, ref spotParams);
					Backend.BindShaderVariable(SpotLightParams.HandleCameraPosition, ref camera.Position);

					Backend.DrawMesh(UnitSphere.Handles[0]);

					Backend.EndInstance();
				}
			}
		}
	}
}
