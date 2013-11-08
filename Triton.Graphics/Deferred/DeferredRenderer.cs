using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Deferred
{
	public class DeferredRenderer
	{
		private readonly Common.ResourceManager ResourceManager;
		private readonly Backend Backend;

		private AmbientLightParams AmbientLightParams = new AmbientLightParams();
		private LightParams PointLightParams = new LightParams();
		private LightParams PointLightShadowParams = new LightParams();
		private LightParams DirectionalLightParams = new LightParams();
		private LightParams SpotLightParams = new LightParams();
		private LightParams SpotLightShadowParams = new LightParams();
		private SSAOParams SSAOParams = new SSAOParams();
		private BlurParams BlurParams = new BlurParams();
		private CombineParams CombineParams = new CombineParams();
		private RenderShadowsParams RenderShadowsParams = new RenderShadowsParams();
		private RenderShadowsParams RenderShadowsCubeParams = new RenderShadowsParams();

		private Vector2 ScreenSize;

		private RenderTarget GBuffer;
		private RenderTarget LightAccumulation;
		private RenderTarget Output;
		private RenderTarget SSAOTarget1;
		private RenderTarget SSAOTarget2;
		private RenderTarget SpotShadowsRenderTarget;

		private BatchBuffer QuadMesh;
		private Resources.Mesh UnitSphere;
		private Resources.Mesh UnitCone;

		private Resources.ShaderProgram AmbientLightShader;
		private Resources.ShaderProgram DirectionalLightShader;
		private Resources.ShaderProgram PointLightShader;
		private Resources.ShaderProgram PointLightShadowShader;
		private Resources.ShaderProgram SpotLightShader;
		private Resources.ShaderProgram SpotLightShadowShader;
		private Resources.ShaderProgram SSAOShader;
		private Resources.ShaderProgram BlurShader;
		private Resources.ShaderProgram CombineShader;
		private Resources.ShaderProgram RenderShadowsShader;
		private Resources.ShaderProgram RenderShadowsCubeShader;

		// Used for point light shadows
		private Light ShadowSpotLight = new Light();

		private Resources.Texture RandomNoiseTexture;

		private bool HandlesInitialized = false;

		private Vector4[] BlurWeights = new Vector4[15];
		private Vector4[] BlurOffsetsHorz = new Vector4[15];
		private Vector4[] BlurOffsetsVert = new Vector4[15];

		public DeferredRenderer(Common.ResourceManager resourceManager, Backend backend, int width, int height)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (backend == null)
				throw new ArgumentNullException("backend");

			ResourceManager = resourceManager;
			Backend = backend;

			ScreenSize = new Vector2(width, height);

			// GBuffer = Backend.CreateRenderTarget("gbuffer", width, height, Triton.Renderer.PixelInternalFormat.Rgba32f, 4, true);
			GBuffer = Backend.CreateRenderTarget("gbuffer", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba32f, Renderer.PixelType.Float, 0),
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba32f, Renderer.PixelType.Float, 1),
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba32f, Renderer.PixelType.Float, 2),
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba32f, Renderer.PixelType.Float, 3),
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.Depth24Stencil8, Renderer.PixelType.Float, 0)
			}));

			LightAccumulation = Backend.CreateRenderTarget("light_accumulation", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba32f, Renderer.PixelType.Float, 0),
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.Depth24Stencil8, Renderer.PixelType.Float, GBuffer.Handle)
			}));

			Output = Backend.CreateRenderTarget("deferred_output", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba32f, Renderer.PixelType.Float, 0),
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.Depth24Stencil8, Renderer.PixelType.Float, GBuffer.Handle)
			}));

			int ssaoScale = 1;
			SSAOTarget1 = Backend.CreateRenderTarget("ssao1", new Definition(width / ssaoScale, height / ssaoScale, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba32f, Renderer.PixelType.Float, 0),
			}));
			SSAOTarget2 = Backend.CreateRenderTarget("ssao2", new Definition(width / ssaoScale, height / ssaoScale, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba32f, Renderer.PixelType.Float, 0),
			}));

			SpotShadowsRenderTarget = Backend.CreateRenderTarget("spot_shadows", new Definition(512, 512, true, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
			}));
			
			AmbientLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/ambient");
			DirectionalLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light");
			PointLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light", "POINT_LIGHT");
			PointLightShadowShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light", "POINT_LIGHT,SHADOWS");
			SpotLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light", "SPOT_LIGHT");
			SpotLightShadowShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light", "SPOT_LIGHT,SHADOWS");
			SSAOShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/ssao");
			BlurShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/blur");
			CombineShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/deferred/combine");
			RenderShadowsShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/deferred/render_shadows");
			RenderShadowsCubeShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/deferred/render_shadows_cube");

			RandomNoiseTexture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/random_n");

			QuadMesh = Backend.CreateBatchBuffer();
			QuadMesh.Begin();
			QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			QuadMesh.End();

			UnitSphere = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/unit_sphere");
			UnitCone = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/unit_cone");
			BlurHelper.Init(ref BlurWeights, ref BlurOffsetsHorz, ref BlurOffsetsVert, new Vector2(1.0f / (float)SSAOTarget1.Width, 1.0f / (float)SSAOTarget1.Height));
		}

		public void InitializeHandles()
		{
			CombineShader.GetUniformLocations(CombineParams);
			AmbientLightShader.GetUniformLocations(AmbientLightParams);
			DirectionalLightShader.GetUniformLocations(DirectionalLightParams);
			PointLightShader.GetUniformLocations(PointLightParams);
			PointLightShadowShader.GetUniformLocations(PointLightShadowParams);
			SpotLightShader.GetUniformLocations(SpotLightParams);
			SpotLightShadowShader.GetUniformLocations(SpotLightShadowParams);
			SSAOShader.GetUniformLocations(SSAOParams);
			BlurShader.GetUniformLocations(BlurParams);
			RenderShadowsShader.GetUniformLocations(RenderShadowsParams);
			RenderShadowsCubeShader.GetUniformLocations(RenderShadowsCubeParams);
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
			RenderLights(camera, ref view, ref projection, stage.GetLights(), stage);

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
			Backend.BeginInstance(BlurShader.Handle, new int[] { SSAOTarget2.Textures[0].Handle });
			Backend.BindShaderVariable(BlurParams.HandleModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(BlurParams.HandleSceneTexture, 0);
			Backend.BindShaderVariable(BlurParams.SampleWeights, ref BlurWeights);
			Backend.BindShaderVariable(BlurParams.HandleSampleOffsets, ref BlurOffsetsHorz);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Blur 2
			Backend.BeginPass(SSAOTarget2, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(BlurShader.Handle, new int[] { SSAOTarget1.Textures[0].Handle });
			Backend.BindShaderVariable(BlurParams.HandleModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(BlurParams.HandleSceneTexture, 0);
			Backend.BindShaderVariable(BlurParams.SampleWeights, ref BlurWeights);
			Backend.BindShaderVariable(BlurParams.HandleSampleOffsets, ref BlurOffsetsVert);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
		}

		private void RenderScene(Stage stage, ref Matrix4 view, ref Matrix4 projection)
		{
			var modelViewProjection = Matrix4.Identity;

			var meshes = stage.GetMeshes();
			foreach (var mesh in meshes)
			{
				var world = Matrix4.Rotate(mesh.Orientation) * Matrix4.CreateTranslation(mesh.Position);
				modelViewProjection = world * view * projection;

				var worldView = world * view;
				var itWorldView = Matrix4.Transpose(Matrix4.Invert(worldView));

				foreach (var subMesh in mesh.Mesh.SubMeshes)
				{
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

		private void RenderLights(Camera camera, ref Matrix4 view, ref Matrix4 projection, IReadOnlyCollection<Light> lights, Stage stage)
		{
			Matrix4 modelViewProjection = Matrix4.Identity;
			Vector3 cameraPositionViewSpace;
			Vector3.Transform(ref camera.Position, ref view, out cameraPositionViewSpace);

			foreach (var light in lights)
			{
				if (!light.Enabled)
					continue;

				if (light.Type == LighType.PointLight && light.CastShadows)
				{
					ShadowSpotLight.Type = LighType.SpotLight;
					ShadowSpotLight.Position = light.Position;
					ShadowSpotLight.Range = light.Range;
					ShadowSpotLight.CastShadows = true;
					ShadowSpotLight.ShadowBias = light.ShadowBias;
					ShadowSpotLight.Intensity = light.Intensity;

					ShadowSpotLight.Color = light.Color;

					ShadowSpotLight.InnerAngle = OpenTK.MathHelper.PiOver2 - 0.001f;
					ShadowSpotLight.OuterAngle = OpenTK.MathHelper.PiOver2 + 0.32f;

					ShadowSpotLight.Direction = Vector3.UnitX;
					RenderLight(camera, ref view, ref projection, stage, ref modelViewProjection, ref cameraPositionViewSpace, ShadowSpotLight);

					ShadowSpotLight.Direction = -Vector3.UnitX;
					RenderLight(camera, ref view, ref projection, stage, ref modelViewProjection, ref cameraPositionViewSpace, ShadowSpotLight);

					ShadowSpotLight.Direction = Vector3.UnitY;
					RenderLight(camera, ref view, ref projection, stage, ref modelViewProjection, ref cameraPositionViewSpace, ShadowSpotLight);

					ShadowSpotLight.Direction = -Vector3.UnitY;
					RenderLight(camera, ref view, ref projection, stage, ref modelViewProjection, ref cameraPositionViewSpace, ShadowSpotLight);

					ShadowSpotLight.Direction = Vector3.UnitZ;
					RenderLight(camera, ref view, ref projection, stage, ref modelViewProjection, ref cameraPositionViewSpace, ShadowSpotLight);

					ShadowSpotLight.Direction = -Vector3.UnitZ;
					RenderLight(camera, ref view, ref projection, stage, ref modelViewProjection, ref cameraPositionViewSpace, ShadowSpotLight);
				}
				else
				{
					RenderLight(camera, ref view, ref projection, stage, ref modelViewProjection, ref cameraPositionViewSpace, light);
				}
			}
		}

		private void RenderLight(Camera camera, ref Matrix4 view, ref Matrix4 projection, Stage stage, ref Matrix4 modelViewProjection, ref Vector3 cameraPositionViewSpace, Light light)
		{
			// Pad the radius of the rendered sphere a little, it's quite low poly so there will be minor artifacts otherwise
			var radius = light.Range * 1.1f;

			var cullFaceMode = Triton.Renderer.CullFaceMode.Back;
			var depthFunction = Triton.Renderer.DepthFunction.Lequal;

			var cameraDistanceToLight = light.Position - camera.Position;

			if (light.Type == LighType.PointLight)
			{
				// We pad it once again to avoid any artifacted when the camera is close to the edge of the bounding sphere
				if (cameraDistanceToLight.Length <= radius * 1.1f)
				{
					cullFaceMode = Triton.Renderer.CullFaceMode.Front;
					depthFunction = Renderer.DepthFunction.Gequal;
				}
			}
			else if (light.Type == LighType.SpotLight)
			{
				if (IsInsideSpotLight(light, camera))
				{
					cullFaceMode = Triton.Renderer.CullFaceMode.Front;
					depthFunction = Renderer.DepthFunction.Gequal;
				}
			}

			// Initialize shadow map
			Matrix4 shadowViewProjection;
			Vector2 shadowCameraClipPlane;

			if (light.CastShadows)
			{
				RenderSpotlightShadows(SpotShadowsRenderTarget, light, stage, camera, out shadowViewProjection, out shadowCameraClipPlane);
				Backend.ChangeRenderTarget(LightAccumulation);
			}
			else
			{
				shadowViewProjection = Matrix4.Identity;
				shadowCameraClipPlane = Vector2.Zero;
			}

			// Calculate matrices
			var scaleMatrix = Matrix4.Scale(radius);

			if (light.Type == LighType.SpotLight)
			{
				var height = light.Range;
				var spotRadius = (float)System.Math.Tan(light.OuterAngle / 2.0f) * height;
				scaleMatrix = Matrix4.Scale(spotRadius, height, spotRadius);
			}

			Matrix4 world;

			if (light.Type == LighType.SpotLight)
			{
				world = (scaleMatrix * Matrix4.Rotate(Vector3.GetRotationTo(Vector3.UnitY, light.Direction))) * Matrix4.CreateTranslation(light.Position);
			}
			else
			{
				world = scaleMatrix * Matrix4.CreateTranslation(light.Position);
			}
			modelViewProjection = world * view * projection;

			// Convert light color to linear space
			var lightColor = light.Color * light.Intensity;
			lightColor = new Vector3((float)System.Math.Pow(lightColor.X, 2.2f), (float)System.Math.Pow(lightColor.Y, 2.2f), (float)System.Math.Pow(lightColor.Z, 2.2f));

			// Select the correct shader
			var shader = DirectionalLightShader;
			var shaderParams = DirectionalLightParams;

			if (light.Type == LighType.PointLight)
			{
				shader = light.CastShadows ? PointLightShadowShader : PointLightShader;
				shaderParams = light.CastShadows ? PointLightShadowParams : PointLightParams;
			}
			else if (light.Type == LighType.SpotLight)
			{
				shader = light.CastShadows ? SpotLightShadowShader : SpotLightShader;
				shaderParams = light.CastShadows ? SpotLightShadowParams : SpotLightParams;
			}

			// Setup textures and begin rendering with the chosen shader
			int[] textures;
			if (light.CastShadows)
			{
				textures = new int[] { GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, GBuffer.Textures[0].Handle, SpotShadowsRenderTarget.Textures[0].Handle };
			}
			else
			{
				textures = new int[] { GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, GBuffer.Textures[0].Handle };
			}

			var depthCheck = light.Type != LighType.Directional;
			Backend.BeginInstance(shader.Handle, textures, true, false, depthCheck, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, cullFaceMode, true, depthFunction);

			// Setup texture samplers
			Backend.BindShaderVariable(shaderParams.HandleNormalTexture, 0);
			Backend.BindShaderVariable(shaderParams.HandlePositionTexture, 1);
			Backend.BindShaderVariable(shaderParams.HandleSpecularTexture, 2);
			Backend.BindShaderVariable(shaderParams.HandleDiffuseTexture, 3);

			// Common uniforms
			Backend.BindShaderVariable(shaderParams.HandleScreenSize, ref ScreenSize);
			Backend.BindShaderVariable(shaderParams.HandleModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(shaderParams.HandleLightColor, ref lightColor);
			Backend.BindShaderVariable(shaderParams.HandleCameraPosition, ref cameraPositionViewSpace);

			if (light.Type == LighType.Directional || light.Type == LighType.SpotLight)
			{
				var lightDirWS = light.Direction.Normalize();

				var lightDirection = Vector3.Transform(light.Direction, Matrix4.Transpose(Matrix4.Invert(view)));
				lightDirection = lightDirection.Normalize();

				Backend.BindShaderVariable(shaderParams.HandleLightDirection, ref lightDirection);
			}

			if (light.Type == LighType.PointLight || light.Type == LighType.SpotLight)
			{
				Vector3 lightPosition;
				Vector3.Transform(ref light.Position, ref view, out lightPosition);

				Backend.BindShaderVariable(shaderParams.HandleLightPosition, ref lightPosition);
				Backend.BindShaderVariable(shaderParams.HandleLightRange, light.Range);
			}

			if (light.Type == LighType.SpotLight)
			{
				var spotParams = new Vector2((float)System.Math.Cos(light.InnerAngle / 2.0f), (float)System.Math.Cos(light.OuterAngle / 2.0f));
				Backend.BindShaderVariable(shaderParams.HandleSpotLightParams, ref spotParams);
			}

			if (light.CastShadows)
			{
				var inverseViewMatrix = Matrix4.Invert(view);

				Backend.BindShaderVariable(shaderParams.HandleShadowMap, 4);
				Backend.BindShaderVariable(shaderParams.HandleInverseViewMatrix, ref inverseViewMatrix);
				Backend.BindShaderVariable(shaderParams.ShadowViewProjection, ref shadowViewProjection);
				Backend.BindShaderVariable(shaderParams.HandleClipPlane, ref shadowCameraClipPlane);
				Backend.BindShaderVariable(shaderParams.HandleShadowBias, light.ShadowBias);
			}

			if (light.Type == LighType.Directional)
			{
				Backend.DrawMesh(QuadMesh.MeshHandle);
			}
			else if (light.Type == LighType.PointLight)
			{
				Backend.DrawMesh(UnitSphere.SubMeshes[0].Handle);
			}
			else
			{
				Backend.DrawMesh(UnitCone.SubMeshes[0].Handle);
			}
			
			Backend.EndInstance();
		}

		bool IsInsideSpotLight(Light light, Camera camera)
		{
			var lightPos = light.Position;
			var lightDir = light.Direction;
			var attAngle = light.OuterAngle;

			var clipRangeFix = -lightDir * (camera.NearClipDistance / (float)System.Math.Tan(attAngle / 2.0f));
			lightPos = lightPos + clipRangeFix;

			var lightToCamDir = camera.Position - lightPos;
			float distanceFromLight = lightToCamDir.Length;
			lightToCamDir = lightToCamDir.Normalize();

			float cosAngle = Vector3.Dot(lightToCamDir, lightDir);
			var angle = (float)System.Math.Acos(cosAngle);

			return (distanceFromLight <= (light.Range + clipRangeFix.Length)) && angle <= attAngle;
		}

		private void RenderSpotlightShadows(RenderTarget renderTarget, Light light, Stage stage, Camera camera, out Matrix4 viewProjection, out Vector2 clipPlane)
		{
			Backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 1), true);

			var modelViewProjection = Matrix4.Identity;

			var orientation = Vector3.GetRotationTo(Vector3.UnitY, light.Direction);

			clipPlane = new Vector2(camera.NearClipDistance, light.Range);

			var view = Matrix4.LookAt(light.Position, light.Position + light.Direction, Vector3.UnitY);
			var projection = Matrix4.CreatePerspectiveFieldOfView(light.OuterAngle, renderTarget.Width / renderTarget.Height, clipPlane.X, clipPlane.Y);

			viewProjection = view * projection;

			var meshes = stage.GetMeshes();
			foreach (var mesh in meshes)
			{
				var world = Matrix4.Rotate(mesh.Orientation) * Matrix4.CreateTranslation(mesh.Position);
				var worldView = world * view;

				modelViewProjection = world * view * projection;

				foreach (var subMesh in mesh.Mesh.SubMeshes)
				{
					Backend.BeginInstance(RenderShadowsShader.Handle, new int[] { }, false, true, true);
					Backend.BindShaderVariable(RenderShadowsParams.ModelViewProjection, ref modelViewProjection);
					Backend.BindShaderVariable(RenderShadowsParams.HandleClipPlane, ref clipPlane);

					Backend.DrawMesh(subMesh.Handle);

					Backend.EndInstance();
				}
			}

			Backend.EndPass();
		}
	}
}
