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
		private LightParams DirectionalLightShadowParams = new LightParams();
		private LightParams SpotLightParams = new LightParams();
		private LightParams SpotLightShadowParams = new LightParams();
		private SSAOParams SSAOParams = new SSAOParams();
		private BlurParams BlurParams = new BlurParams();
		private CombineParams CombineParams = new CombineParams();
		private RenderShadowsParams RenderShadowsParams = new RenderShadowsParams();
		private RenderShadowsParams RenderShadowsCubeParams = new RenderShadowsParams();
		private RenderShadowsParams RenderShadowsSkinnedParams = new RenderShadowsParams();

		private Vector2 ScreenSize;

		public readonly RenderTarget GBuffer;
		private readonly RenderTarget LightAccumulation;
		private readonly RenderTarget Output;
		private readonly RenderTarget SSAOTarget1;
		private readonly RenderTarget SSAOTarget2;
		private readonly RenderTarget SpotShadowsRenderTarget;
		private readonly RenderTarget PointShadowsRenderTarget;
		public readonly RenderTarget DirectionalShadowsRenderTarget;

		private BatchBuffer QuadMesh;
		private Resources.Mesh UnitSphere;
		private Resources.Mesh UnitCone;

		private Resources.ShaderProgram AmbientLightShader;
		private Resources.ShaderProgram DirectionalLightShader;
		private Resources.ShaderProgram DirectionalLightShadowShader;
		private Resources.ShaderProgram PointLightShader;
		private Resources.ShaderProgram PointLightShadowShader;
		private Resources.ShaderProgram SpotLightShader;
		private Resources.ShaderProgram SpotLightShadowShader;
		private Resources.ShaderProgram SSAOShader;
		private Resources.ShaderProgram BlurShader;
		private Resources.ShaderProgram CombineShader;
		private Resources.ShaderProgram RenderShadowsShader;
		private Resources.ShaderProgram RenderShadowsCubeShader;
		private Resources.ShaderProgram RenderShadowsSkinnedShader;

		// Used for point light shadows
		private Light ShadowSpotLight = new Light();

		private Resources.Texture RandomNoiseTexture;

		private bool HandlesInitialized = false;

		private Vector4[] BlurWeights = new Vector4[15];
		private Vector4[] BlurOffsetsHorz = new Vector4[15];
		private Vector4[] BlurOffsetsVert = new Vector4[15];

		private int AmbientRenderState;
		private int LightAccumulatinRenderState;
		private int ShadowsRenderState;

		private int DirectionalRenderState;
		private int LightInsideRenderState;
		private int LightOutsideRenderState;

		public DeferredRenderer(Common.ResourceManager resourceManager, Backend backend, int width, int height)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (backend == null)
				throw new ArgumentNullException("backend");

			ResourceManager = resourceManager;
			Backend = backend;

			ScreenSize = new Vector2(width, height);

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

			PointShadowsRenderTarget = Backend.CreateRenderTarget("point_shadows", new Definition(512, 512, true, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
			}, true));

			DirectionalShadowsRenderTarget = Backend.CreateRenderTarget("directional_shadows", new Definition(2048, 2048, true, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
			}));

			AmbientLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/ambient");
			DirectionalLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light");
			DirectionalLightShadowShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light", "SHADOWS");
			PointLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light", "POINT_LIGHT");
			PointLightShadowShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light", "POINT_LIGHT,SHADOWS,SHADOWS_CUBE");
			SpotLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light", "SPOT_LIGHT");
			SpotLightShadowShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/light", "SPOT_LIGHT,SHADOWS");
			SSAOShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred/ssao");
			BlurShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/blur");
			CombineShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/deferred/combine");
			RenderShadowsShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/deferred/render_shadows");
			RenderShadowsSkinnedShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/deferred/render_shadows", "SKINNED");
			RenderShadowsCubeShader = ResourceManager.Load<Resources.ShaderProgram>("shaders/deferred/render_shadows_cube");

			RandomNoiseTexture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/random_n");

			QuadMesh = Backend.CreateBatchBuffer();
			QuadMesh.Begin();
			QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			QuadMesh.End();

			UnitSphere = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/unit_sphere");
			UnitCone = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/unit_cone");
			BlurHelper.Init(ref BlurWeights, ref BlurOffsetsHorz, ref BlurOffsetsVert, new Vector2(1.0f / (float)SSAOTarget1.Width, 1.0f / (float)SSAOTarget1.Height));

			AmbientRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
			LightAccumulatinRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
			ShadowsRenderState = Backend.CreateRenderState(false, true, true);
			DirectionalRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);
			LightInsideRenderState = Backend.CreateRenderState(true, false, true, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Triton.Renderer.CullFaceMode.Front, true, Renderer.DepthFunction.Gequal);
			LightOutsideRenderState = Backend.CreateRenderState(true, false, true, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);
		}

		public void InitializeHandles()
		{
			CombineShader.GetUniformLocations(CombineParams);
			AmbientLightShader.GetUniformLocations(AmbientLightParams);
			DirectionalLightShader.GetUniformLocations(DirectionalLightParams);
			DirectionalLightShadowShader.GetUniformLocations(DirectionalLightShadowParams);
			PointLightShader.GetUniformLocations(PointLightParams);
			PointLightShadowShader.GetUniformLocations(PointLightShadowParams);
			SpotLightShader.GetUniformLocations(SpotLightParams);
			SpotLightShadowShader.GetUniformLocations(SpotLightShadowParams);
			SSAOShader.GetUniformLocations(SSAOParams);
			BlurShader.GetUniformLocations(BlurParams);
			RenderShadowsShader.GetUniformLocations(RenderShadowsParams);
			RenderShadowsSkinnedShader.GetUniformLocations(RenderShadowsSkinnedParams);
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
			Backend.BeginPass(GBuffer, stage.ClearColor, true);
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

			Backend.BeginInstance(CombineShader.Handle, new int[] { LightAccumulation.Textures[0].Handle, SSAOTarget2.Textures[0].Handle }, LightAccumulatinRenderState);
			Backend.BindShaderVariable(CombineParams.SamplerLight, 0);
			Backend.BindShaderVariable(CombineParams.SamplerSSAO, 1);

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
			Backend.BindShaderVariable(SSAOParams.ModelViewProjection, ref modelViewProjection);

			Backend.BindShaderVariable(SSAOParams.SamplerPosition, 0);
			Backend.BindShaderVariable(SSAOParams.SamplerNormal, 1);
			Backend.BindShaderVariable(SSAOParams.SamplerRandom, 2);

			var noiseScale = new Vector2(ScreenSize.X / 64, ScreenSize.Y / 64);
			Backend.BindShaderVariable(SSAOParams.NoiseScale, ref noiseScale);

			Backend.DrawMesh(QuadMesh.MeshHandle);

			Backend.EndPass();

			// Blur 1
			Backend.BeginPass(SSAOTarget1, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(BlurShader.Handle, new int[] { SSAOTarget2.Textures[0].Handle });
			Backend.BindShaderVariable(BlurParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(BlurParams.SamplerScene, 0);
			Backend.BindShaderVariable(BlurParams.SampleWeights, ref BlurWeights);
			Backend.BindShaderVariable(BlurParams.SampleOffsets, ref BlurOffsetsHorz);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Blur 2
			Backend.BeginPass(SSAOTarget2, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(BlurShader.Handle, new int[] { SSAOTarget1.Textures[0].Handle });
			Backend.BindShaderVariable(BlurParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(BlurParams.SamplerScene, 0);
			Backend.BindShaderVariable(BlurParams.SampleWeights, ref BlurWeights);
			Backend.BindShaderVariable(BlurParams.SampleOffsets, ref BlurOffsetsVert);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
		}

		private void RenderScene(Stage stage, ref Matrix4 view, ref Matrix4 projection)
		{
			var modelViewProjection = Matrix4.Identity;

			var meshes = stage.GetMeshes();
			foreach (var mesh in meshes)
			{
				var world = mesh.World;
				modelViewProjection = world * view * projection;

				var worldView = world * view;
				var itWorldView = Matrix4.Transpose(Matrix4.Invert(worldView));

				foreach (var subMesh in mesh.Mesh.SubMeshes)
				{
					subMesh.Material.BindMaterial(Backend, ref world, ref worldView, ref itWorldView, ref modelViewProjection, mesh.Skeleton);
					Backend.DrawMesh(subMesh.Handle);
					Backend.EndInstance();
				}
			}
		}

		private void RenderAmbientLight(Stage stage)
		{
			Matrix4 modelViewProjection = Matrix4.Identity;

			var ambientColor = new Vector3((float)System.Math.Pow(stage.AmbientColor.X, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Y, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Z, 2.2f));

			Backend.BeginInstance(AmbientLightShader.Handle, new int[] { GBuffer.Textures[0].Handle }, AmbientRenderState);
			Backend.BindShaderVariable(AmbientLightParams.SamplerDiffuse, 0);
			Backend.BindShaderVariable(AmbientLightParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(AmbientLightParams.AmbientColor, ref ambientColor);

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

				RenderLight(camera, ref view, ref projection, stage, ref modelViewProjection, ref cameraPositionViewSpace, light);
			}
		}

		private void RenderLight(Camera camera, ref Matrix4 view, ref Matrix4 projection, Stage stage, ref Matrix4 modelViewProjection, ref Vector3 cameraPositionViewSpace, Light light)
		{
			// Pad the radius of the rendered sphere a little, it's quite low poly so there will be minor artifacts otherwise
			var radius = light.Range * 1.1f;

			var renderStateId = DirectionalRenderState;

			var cameraDistanceToLight = light.Position - camera.Position;

			if (light.Type == LighType.PointLight)
			{
				renderStateId = LightOutsideRenderState;
				// We pad it once again to avoid any artifacted when the camera is close to the edge of the bounding sphere
				if (cameraDistanceToLight.Length <= radius * 1.1f)
				{
					renderStateId = LightInsideRenderState;
				}
			}
			else if (light.Type == LighType.SpotLight)
			{
				renderStateId = LightOutsideRenderState;
				if (IsInsideSpotLight(light, camera))
				{
					renderStateId = LightInsideRenderState;
				}
			}

			// Initialize shadow map
			Matrix4 shadowViewProjection;
			Vector2 shadowCameraClipPlane;

			if (light.CastShadows)
			{
				if (light.Type == LighType.PointLight)
				{
					RenderShadowsCube(PointShadowsRenderTarget, light, stage, camera, out shadowViewProjection, out shadowCameraClipPlane);
				}
				else
				{
					RenderShadows(light.Type == LighType.Directional ? DirectionalShadowsRenderTarget : SpotShadowsRenderTarget, light, stage, camera, out shadowViewProjection, out shadowCameraClipPlane);
				}
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
			var shader = light.CastShadows ? DirectionalLightShadowShader : DirectionalLightShader;
			var shaderParams = light.CastShadows ? DirectionalLightShadowParams : DirectionalLightParams;

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
				var shadowMapHandle = SpotShadowsRenderTarget.Textures[0].Handle;
				if (light.Type == LighType.Directional)
					shadowMapHandle = DirectionalShadowsRenderTarget.Textures[0].Handle;
				else if (light.Type == LighType.PointLight)
					shadowMapHandle = PointShadowsRenderTarget.Textures[0].Handle;
				textures = new int[] { GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, GBuffer.Textures[0].Handle, shadowMapHandle };
			}
			else
			{
				textures = new int[] { GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, GBuffer.Textures[0].Handle };
			}

			Backend.BeginInstance(shader.Handle, textures, renderStateId);

			// Setup texture samplers
			Backend.BindShaderVariable(shaderParams.SamplerNormal, 0);
			Backend.BindShaderVariable(shaderParams.SamplerPosition, 1);
			Backend.BindShaderVariable(shaderParams.SamplerSpecular, 2);
			Backend.BindShaderVariable(shaderParams.SamplerDiffuse, 3);

			// Common uniforms
			Backend.BindShaderVariable(shaderParams.ScreenSize, ref ScreenSize);
			Backend.BindShaderVariable(shaderParams.ModelViewProjection, ref modelViewProjection);
			Backend.BindShaderVariable(shaderParams.LightColor, ref lightColor);
			Backend.BindShaderVariable(shaderParams.CameraPosition, ref cameraPositionViewSpace);

			if (light.Type == LighType.Directional || light.Type == LighType.SpotLight)
			{
				var lightDirWS = light.Direction.Normalize();

				var lightDirection = Vector3.Transform(light.Direction, Matrix4.Transpose(Matrix4.Invert(view)));
				lightDirection = lightDirection.Normalize();

				Backend.BindShaderVariable(shaderParams.LightDirection, ref lightDirection);
			}

			if (light.Type == LighType.PointLight || light.Type == LighType.SpotLight)
			{
				Vector3 lightPosition;
				Vector3.Transform(ref light.Position, ref view, out lightPosition);

				Backend.BindShaderVariable(shaderParams.LightPosition, ref lightPosition);
				Backend.BindShaderVariable(shaderParams.LightRange, light.Range);
			}

			if (light.Type == LighType.SpotLight)
			{
				var spotParams = new Vector2((float)System.Math.Cos(light.InnerAngle / 2.0f), (float)System.Math.Cos(light.OuterAngle / 2.0f));
				Backend.BindShaderVariable(shaderParams.SpotParams, ref spotParams);
			}

			if (light.CastShadows)
			{
				var inverseViewMatrix = Matrix4.Invert(view);

				Backend.BindShaderVariable(shaderParams.InvView, ref inverseViewMatrix);
				Backend.BindShaderVariable(shaderParams.ClipPlane, ref shadowCameraClipPlane);
				Backend.BindShaderVariable(shaderParams.ShadowBias, light.ShadowBias);

				var texelSize = 1.0f / (light.Type == LighType.Directional ? DirectionalShadowsRenderTarget.Width : SpotShadowsRenderTarget.Width);
				Backend.BindShaderVariable(shaderParams.TexelSize, texelSize);

				if (light.Type == LighType.PointLight)
				{
					Backend.BindShaderVariable(shaderParams.SamplerShadowCube, 4);
					
				}
				else
				{
					Backend.BindShaderVariable(shaderParams.SamplerShadow, 4);
					Backend.BindShaderVariable(shaderParams.ShadowViewProj, ref shadowViewProjection);
				}
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

		private void RenderShadows(RenderTarget renderTarget, Light light, Stage stage, Camera camera, out Matrix4 viewProjection, out Vector2 clipPlane)
		{
			Backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 1), true);

			var modelViewProjection = Matrix4.Identity;

			var orientation = Vector3.GetRotationTo(Vector3.UnitY, light.Direction);

			clipPlane = new Vector2(light.ShadowNearClipDistance, light.Range * 2.0f);

			Matrix4 view, projection;
			if (light.Type == LighType.Directional)
			{
				view = Matrix4.LookAt(camera.Position - light.Direction * light.Range, camera.Position, Vector3.UnitY);
				projection = Matrix4.CreatePerspectiveFieldOfView(Math.Util.DegreesToRadians(40), renderTarget.Width / (float)renderTarget.Height, clipPlane.X, clipPlane.Y);
			}
			else
			{
				view = Matrix4.LookAt(light.Position, light.Position + light.Direction, Vector3.UnitY);
				projection = Matrix4.CreatePerspectiveFieldOfView(light.OuterAngle, renderTarget.Width / (float)renderTarget.Height, clipPlane.X, clipPlane.Y);
			}

			viewProjection = view * projection;

			var meshes = stage.GetMeshes();
			foreach (var mesh in meshes)
			{
				var world = mesh.World;

				modelViewProjection = world * view * projection;

				foreach (var subMesh in mesh.Mesh.SubMeshes)
				{
					Backend.BeginInstance(RenderShadowsShader.Handle, new int[] { }, ShadowsRenderState);
					Backend.BindShaderVariable(RenderShadowsParams.ModelViewProjection, ref modelViewProjection);
					Backend.BindShaderVariable(RenderShadowsParams.ClipPlane, ref clipPlane);

					Backend.DrawMesh(subMesh.Handle);

					Backend.EndInstance();
				}
			}

			Backend.EndPass();
		}

		private void RenderShadowsCube(RenderTarget renderTarget, Light light, Stage stage, Camera camera, out Matrix4 viewProjection, out Vector2 clipPlane)
		{
			Backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 1), true);

			var modelViewProjection = Matrix4.Identity;

			var orientation = Vector3.GetRotationTo(Vector3.UnitY, light.Direction);

			clipPlane = new Vector2(light.ShadowNearClipDistance, light.Range * 2.0f);

			var projection = Matrix4.CreatePerspectiveFieldOfView(OpenTK.MathHelper.DegreesToRadians(90), renderTarget.Width / (float)renderTarget.Height, clipPlane.X, clipPlane.Y);

			var viewProjectionMatrices = new Matrix4[]
			{
				(Matrix4.CreateTranslation(-light.Position) * new Matrix4(0, 0, -1, 0, 0, -1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 1)) * projection,
				(Matrix4.CreateTranslation(-light.Position) * new Matrix4(0, 0, 1, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1)) * projection,

				(Matrix4.CreateTranslation(-light.Position) * new Matrix4(1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1)) * projection,
				(Matrix4.CreateTranslation(-light.Position) * new Matrix4(1, 0, 0, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 1)) * projection,
				
				(Matrix4.CreateTranslation(-light.Position) * new Matrix4(1, 0, 0, 0, 0, -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1)) * projection,
				(Matrix4.CreateTranslation(-light.Position) * new Matrix4(-1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)) * projection
			};

			//var viewProjectionMatrices = new Matrix4[]
			//{
			//	Matrix4.LookAt(light.Position, light.Position + Vector3.UnitX, Vector3.UnitY) * projection,
			//	Matrix4.LookAt(light.Position, light.Position + -Vector3.UnitX, Vector3.UnitY) * projection,

			//	Matrix4.LookAt(light.Position, light.Position + Vector3.UnitY, Vector3.UnitX) * projection,
			//	Matrix4.LookAt(light.Position, light.Position + -Vector3.UnitY, Vector3.UnitX) * projection,

			//	Matrix4.LookAt(light.Position, light.Position + Vector3.UnitZ, Vector3.UnitY) * projection,
			//	Matrix4.LookAt(light.Position, light.Position + -Vector3.UnitZ, Vector3.UnitY) * projection,
			//};

			viewProjection = projection;

			var meshes = stage.GetMeshes();
			foreach (var mesh in meshes)
			{
				var world = mesh.World;

				foreach (var subMesh in mesh.Mesh.SubMeshes)
				{
					Backend.BeginInstance(RenderShadowsCubeShader.Handle, new int[] { }, ShadowsRenderState);
					Backend.BindShaderVariable(RenderShadowsCubeParams.Model, ref world);
					Backend.BindShaderVariable(RenderShadowsCubeParams.ClipPlane, ref clipPlane);
					Backend.BindShaderVariable(RenderShadowsCubeParams.ViewProjectionMatrices, ref viewProjectionMatrices);
					Backend.BindShaderVariable(RenderShadowsCubeParams.LightPosition, ref light.Position);

					Backend.DrawMesh(subMesh.Handle);

					Backend.EndInstance();
				}
			}

			Backend.EndPass();
		}
	}
}
