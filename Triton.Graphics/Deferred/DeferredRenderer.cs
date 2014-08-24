using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Deferred
{
	public class DeferredRenderer
	{
		private readonly Common.ResourceManager ResourceManager;
		private readonly Backend Backend;

		private AmbientLightParams AmbientLightParams = new AmbientLightParams();
		private LightParams[] LightParams;
		private CombineParams CombineParams = new CombineParams();
		private RenderShadowsParams RenderShadowsParams = new RenderShadowsParams();
		private RenderShadowsParams RenderShadowsCubeParams = new RenderShadowsParams();
		private RenderShadowsParams RenderShadowsSkinnedParams = new RenderShadowsParams();
		private FXAAParams FXAAParams = new FXAAParams();
		private FogParams FogParams = new FogParams();

		private Vector2 ScreenSize;

		public readonly RenderTarget GBuffer;
		private readonly RenderTarget LightAccumulation;
		private readonly RenderTarget Temporary;
		private readonly RenderTarget Output;
		private readonly RenderTarget SpotShadowsRenderTarget;
		private readonly RenderTarget PointShadowsRenderTarget;
		public readonly RenderTarget DirectionalShadowsRenderTarget;

		private BatchBuffer QuadMesh;
		private Resources.Mesh UnitSphere;
		private Resources.Mesh UnitCone;

		private Resources.ShaderProgram AmbientLightShader;
		private Resources.ShaderProgram[] LightShaders;
		private Resources.ShaderProgram CombineShader;
		private Resources.ShaderProgram RenderShadowsShader;
		private Resources.ShaderProgram RenderShadowsCubeShader;
		private Resources.ShaderProgram RenderShadowsSkinnedShader;
		private Resources.ShaderProgram FXAAShader;
		private Resources.ShaderProgram FogShader;

		// Used for point light shadows
		private Light ShadowSpotLight = new Light();

		private Resources.Texture RandomNoiseTexture;
		private Resources.Texture EnvironmentMap;
		private Resources.Texture EnvironmentMapSpecular;

		private bool HandlesInitialized = false;

		private int SkyRenderState;
		private int AmbientRenderState;
		private int LightAccumulatinRenderState;
		private int ShadowsRenderState;

		private int DirectionalRenderState;
		private int LightInsideRenderState;
		private int LightOutsideRenderState;

		private int ShadowSampler;

		public bool EnableFXAA = true;
		public bool EnableShadows = true;

		public int DirectionalShaderOffset = 0;
		public int PointLightShaderOffset = 0;
		public int SpotLightShaderOffset = 0;

		public ShadowQuality ShadowQuality = ShadowQuality.High;

		// Mesh list used for rendering, declared here to avoid GC
		private List<MeshInstance> Meshes = new List<MeshInstance>();

		public FogSettings FogSettings = new FogSettings();

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
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 1),
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 2),
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 3),
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 4),
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.Depth24Stencil8, Renderer.PixelType.Float, 0)
			}));

			LightAccumulation = Backend.CreateRenderTarget("light_accumulation", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.Depth24Stencil8, Renderer.PixelType.Float, GBuffer.Handle)
			}));

			Temporary = Backend.CreateRenderTarget("deferred_temp", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.Depth24Stencil8, Renderer.PixelType.Float, GBuffer.Handle)
			}));

			Output = Backend.CreateRenderTarget("deferred_output", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
				new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.Depth24Stencil8, Renderer.PixelType.Float, GBuffer.Handle)
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

			AmbientLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("/shaders/deferred/ambient");

			// Init light shaders
			var lightTypes = new string[] { "DIRECTIONAL_LIGHT", "POINT_LIGHT", "SPOT_LIGHT" };
			var lightPermutations = new string[] { "NO_SHADOWS", "SHADOWS,SHADOW_QUALITY_LOWEST", "SHADOWS,SHADOW_QUALITY_LOW", "SHADOWS,SHADOW_QUALITY_MEDIUM", "SHADOWS,SHADOW_QUALITY_HIGH" };

			LightShaders = new Resources.ShaderProgram[lightTypes.Length * lightPermutations.Length];
			LightParams = new LightParams[lightTypes.Length * lightPermutations.Length];

			for (var l = 0; l < lightTypes.Length; l++)
			{
				var lightType = lightTypes[l];
				for (var p = 0; p < lightPermutations.Length; p++)
				{
					var index = l * lightPermutations.Length + p;
					var defines = lightType + "," + lightPermutations[p];

					if (lightType == "POINT_LIGHT")
						defines += ",SHADOWS_CUBE";

					LightShaders[index] = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("/shaders/deferred/light", defines);
					LightParams[index] = new LightParams();
				}
			}

			DirectionalShaderOffset = 0;
			PointLightShaderOffset = 1 * lightPermutations.Length;
			SpotLightShaderOffset = 2 * lightPermutations.Length;

			CombineShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/combine");
			RenderShadowsShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows");
			RenderShadowsSkinnedShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows", "SKINNED");
			RenderShadowsCubeShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows_cube");
			FXAAShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/fxaa");
			FogShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/fog");

			RandomNoiseTexture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("/textures/random_n");
			EnvironmentMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("/textures/sky_ambient");
			EnvironmentMapSpecular = ResourceManager.Load<Triton.Graphics.Resources.Texture>("/textures/sky");

			QuadMesh = Backend.CreateBatchBuffer();
			QuadMesh.Begin();
			QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			QuadMesh.End();

			UnitSphere = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("/models/unit_sphere");
			UnitCone = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("/models/unit_cone");

			AmbientRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
			SkyRenderState = Backend.CreateRenderState(false, false, false);
			LightAccumulatinRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
			ShadowsRenderState = Backend.CreateRenderState(false, true, true);
			DirectionalRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);
			LightInsideRenderState = Backend.CreateRenderState(true, false, true, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Triton.Renderer.CullFaceMode.Front, true, Renderer.DepthFunction.Gequal);
			LightOutsideRenderState = Backend.CreateRenderState(true, false, true, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);

			ShadowSampler = Backend.RenderSystem.CreateSampler(new Dictionary<Renderer.SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMinFilter.Linear },
				{ SamplerParameterName.TextureCompareFunc, (int)DepthFunction.Lequal },
				{ SamplerParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRToTexture },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});
		}

		public void InitializeHandles()
		{
			CombineShader.GetUniformLocations(CombineParams);
			AmbientLightShader.GetUniformLocations(AmbientLightParams);
			for (var i = 0; i < LightParams.Length; i++)
			{
				LightShaders[i].GetUniformLocations(LightParams[i]);
			}
			RenderShadowsShader.GetUniformLocations(RenderShadowsParams);
			RenderShadowsSkinnedShader.GetUniformLocations(RenderShadowsSkinnedParams);
			RenderShadowsCubeShader.GetUniformLocations(RenderShadowsCubeParams);
			FXAAShader.GetUniformLocations(FXAAParams);
			FogShader.GetUniformLocations(FogParams);
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
			var clearColor = stage.ClearColor;
			clearColor.W = 0;
			Backend.BeginPass(GBuffer, clearColor, true);
			RenderScene(stage, camera, ref view, ref projection);
			Backend.EndPass();

			// Render light accumulation
			Backend.BeginPass(LightAccumulation, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), false);

			RenderAmbientLight(stage);
			RenderLights(camera, ref view, ref projection, stage.GetLights(), stage);

			Backend.EndPass();

			var currentRenderTarget = Temporary;
			var currentSource = LightAccumulation;

			if (FogSettings.Enable)
			{
				Backend.BeginPass(currentRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), false);

				Backend.BeginInstance(FogShader.Handle, new int[] { currentSource.Textures[0].Handle, GBuffer.Textures[2].Handle }, new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering }, LightAccumulatinRenderState);
				Backend.BindShaderVariable(FogParams.SamplerScene, 0);
				Backend.BindShaderVariable(FogParams.SamplerGBuffer2, 1);
				Backend.BindShaderVariable(FogParams.FogStart, FogSettings.Start);
				Backend.BindShaderVariable(FogParams.FogEnd, FogSettings.End);
				Backend.BindShaderVariable(FogParams.FogColor, ref FogSettings.Color);

				Vector2 screenSize = new Vector2(LightAccumulation.Width, LightAccumulation.Height);
				Backend.BindShaderVariable(FogParams.ScreenSize, ref screenSize);

				Backend.DrawMesh(QuadMesh.MeshHandle);

				Backend.EndPass();

				var tmp = currentRenderTarget;
				currentRenderTarget = currentSource;
				currentSource = tmp;
			}

			if (EnableFXAA)
			{
				Backend.BeginPass(currentRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), false);

				Backend.BeginInstance(FXAAShader.Handle, new int[] { currentSource.Textures[0].Handle }, new int[] { Backend.DefaultSamplerNoFiltering }, LightAccumulatinRenderState);
				Backend.BindShaderVariable(FXAAParams.SamplerScene, 0);

				Vector2 screenSize = new Vector2(LightAccumulation.Width, LightAccumulation.Height);
				Backend.BindShaderVariable(FXAAParams.TextureSize, ref screenSize);

				Backend.DrawMesh(QuadMesh.MeshHandle);

				Backend.EndPass();

				var tmp = currentRenderTarget;
				currentRenderTarget = currentSource;
				currentSource = tmp;
			}

			Backend.BeginPass(Output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), false);

			Backend.BeginInstance(CombineShader.Handle, new int[] { currentSource.Textures[0].Handle }, new int[] { Backend.DefaultSamplerNoFiltering }, LightAccumulatinRenderState);
			Backend.BindShaderVariable(CombineParams.SamplerLight, 0);
			Backend.DrawMesh(QuadMesh.MeshHandle);

			Backend.EndPass();

			return Output;
		}

		private void RenderScene(Stage stage, Camera camera, ref Matrix4 view, ref Matrix4 projection)
		{
			var modelViewProjection = Matrix4.Identity;

			if (stage.Sky != null)
			{
				var world = Matrix4.CreateTranslation(camera.Position);
				modelViewProjection = world * view * projection;

				var worldView = world * view;
				var itWorldView = Matrix4.Transpose(Matrix4.Invert(worldView));

				foreach (var subMesh in stage.Sky.Mesh.SubMeshes)
				{
					subMesh.Material.BindMaterial(Backend, EnvironmentMap, EnvironmentMapSpecular, camera, ref world, ref worldView, ref itWorldView, ref modelViewProjection, null, SkyRenderState);
					Backend.DrawMesh(subMesh.Handle);
					Backend.EndInstance();
				}
			}

			var meshes = stage.GetMeshes();
			foreach (var mesh in meshes)
			{
				var world = mesh.World;
				modelViewProjection = world * view * projection;

				var worldView = world * view;
				var itWorldView = Matrix4.Transpose(Matrix4.Invert(worldView));

				foreach (var subMesh in mesh.Mesh.SubMeshes)
				{
					subMesh.Material.BindMaterial(Backend, EnvironmentMap, EnvironmentMapSpecular, camera, ref world, ref worldView, ref itWorldView, ref modelViewProjection, mesh.Skeleton, 0);
					Backend.DrawMesh(subMesh.Handle);
					Backend.EndInstance();
				}
			}
		}

		private void RenderAmbientLight(Stage stage)
		{
			Matrix4 modelViewProjection = Matrix4.Identity;

			var ambientColor = new Vector3((float)System.Math.Pow(stage.AmbientColor.X, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Y, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Z, 2.2f));

			Backend.BeginInstance(AmbientLightShader.Handle,
				new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle, GBuffer.Textures[3].Handle, GBuffer.Textures[4].Handle },
				new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering },
				AmbientRenderState);
			Backend.BindShaderVariable(AmbientLightParams.SamplerGBuffer0, 0);
			Backend.BindShaderVariable(AmbientLightParams.SamplerGBuffer1, 1);
			Backend.BindShaderVariable(AmbientLightParams.SamplerGBuffer3, 2);
			Backend.BindShaderVariable(AmbientLightParams.SamplerGBuffer4, 3);
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
			var lightTypeOffset = 0;

			if (light.Type == LighType.PointLight)
				lightTypeOffset = PointLightShaderOffset;
			else if (light.Type == LighType.SpotLight)
				lightTypeOffset = SpotLightShaderOffset;

			if (light.CastShadows && EnableShadows)
				lightTypeOffset += 1 + (int)ShadowQuality;

			var shader = LightShaders[lightTypeOffset];
			var shaderParams = LightParams[lightTypeOffset];

			// Setup textures and begin rendering with the chosen shader
			int[] textures;
			int[] samplers;
			if (light.CastShadows)
			{
				var shadowMapHandle = SpotShadowsRenderTarget.Textures[0].Handle;
				if (light.Type == LighType.Directional)
					shadowMapHandle = DirectionalShadowsRenderTarget.Textures[0].Handle;
				else if (light.Type == LighType.PointLight)
					shadowMapHandle = PointShadowsRenderTarget.Textures[0].Handle;
				textures = new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, shadowMapHandle };
				samplers = new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, ShadowSampler };
			}
			else
			{
				textures = new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle };
				samplers = new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering };
			}

			Backend.BeginInstance(shader.Handle, textures, samplers, renderStateId);

			// Setup texture samplers
			Backend.BindShaderVariable(shaderParams.SamplerGBuffer0, 0);
			Backend.BindShaderVariable(shaderParams.SamplerGBuffer1, 1);
			Backend.BindShaderVariable(shaderParams.SamplerGBuffer2, 2);
			Backend.BindShaderVariable(shaderParams.SamplerGBuffer3, 3);

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
				var cameraFrustum = camera.GetFrustum();
				var lightDir = -light.Direction;
				lightDir.Normalize();

				Matrix4 lightRotation = Matrix4.LookAt(Vector3.Zero, lightDir, Vector3.UnitY);
				Vector3[] corners = cameraFrustum.GetCorners();

				for (var i = 0; i < corners.Length; i++)
				{
					corners[i] = Vector3.Transform(corners[i], lightRotation);
				}

				var lightBox = BoundingBox.CreateFromPoints(corners);
				var boxSize = lightBox.Max - lightBox.Min;
				Vector3 halfBoxSize;
				Vector3.Multiply(ref boxSize, 0.5f, out halfBoxSize);

				var lightPosition = lightBox.Min + halfBoxSize;
				lightPosition.Z = lightBox.Min.Z;

				lightPosition = Vector3.Transform(lightPosition, Matrix4.Invert(lightRotation));

				view = Matrix4.LookAt(lightPosition, lightPosition - lightDir, Vector3.UnitY);
				projection = Matrix4.CreateOrthographic(boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z);
				//clipPlane.X = -boxSize.Z;
				//clipPlane.Y = boxSize.Z;

				//view = Matrix4.LookAt(lightPosition, camera.Position, Vector3.UnitY);
				projection = Matrix4.CreatePerspectiveFieldOfView(Math.Util.DegreesToRadians(20), renderTarget.Width / (float)renderTarget.Height, clipPlane.X, clipPlane.Y);
				//projection = Matrix4.CreateOrthographic(renderTarget.Width, renderTarget.Height, clipPlane.X, clipPlane.Y * 10000);
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
				if (!mesh.CastShadows)
					continue;

				var world = mesh.World;

				modelViewProjection = world * view * projection;

				foreach (var subMesh in mesh.Mesh.SubMeshes)
				{
					Backend.BeginInstance(RenderShadowsShader.Handle, new int[] { }, null, ShadowsRenderState);
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

			viewProjection = projection;

			Meshes.Clear();
			stage.GetMeshesInRadius(light.Position, light.Range, Meshes);
			foreach (var mesh in Meshes)
			{
				if (!mesh.CastShadows)
					continue;

				var world = mesh.World;

				foreach (var subMesh in mesh.Mesh.SubMeshes)
				{
					Backend.BeginInstance(RenderShadowsCubeShader.Handle, new int[] { }, null, ShadowsRenderState);
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
