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
        private RenderShadowsParams RenderShadowsSkinnedCubeParams = new RenderShadowsParams();
        private FogParams FogParams = new FogParams();
        private LightParams _computeLightParams = new LightParams();

        private Vector2 ScreenSize;

        public readonly RenderTarget GBuffer;
        private readonly RenderTarget LightAccumulation;
        private readonly RenderTarget Temporary;
        private readonly RenderTarget Output;
        private readonly RenderTarget SpotShadowsRenderTarget;
        private readonly RenderTarget PointShadowsRenderTarget;
        public readonly RenderTarget[] DirectionalShadowsRenderTarget;

        private BatchBuffer QuadMesh;
        private Resources.Mesh UnitSphere;
        private Resources.Mesh UnitCone;

        private Resources.ShaderProgram AmbientLightShader;
        private Resources.ShaderProgram[] LightShaders;
        private Resources.ShaderProgram CombineShader;
        private Resources.ShaderProgram RenderShadowsShader;
        private Resources.ShaderProgram RenderShadowsCubeShader;
        private Resources.ShaderProgram RenderShadowsSkinnedShader;
        private Resources.ShaderProgram RenderShadowsSkinnedCubeShader;
        private Resources.ShaderProgram FogShader;
        private Resources.ShaderProgram _lightComputeShader;

        private const int NumLightInstances = 2048;
        private readonly PointLightDataCS[] _pointLightDataCS = new PointLightDataCS[NumLightInstances];
        private int _pointLightDataCSBuffer;

        // Used for point light shadows
        private Resources.Texture RandomNoiseTexture;

        private bool HandlesInitialized = false;

        private int AmbientRenderState;
        private int LightAccumulatinRenderState;
        private int ShadowsRenderState;

        private int DirectionalRenderState;
        private int LightInsideRenderState;
        private int LightOutsideRenderState;

        private int ShadowSampler;

        public int DirectionalShaderOffset = 0;
        public int PointLightShaderOffset = 0;
        public int SpotLightShaderOffset = 0;

        public int RenderedLights = 0;

        public FogSettings FogSettings = new FogSettings();

        public RenderSettings Settings;

        private readonly RenderOperations RenderOperations = new RenderOperations();
        private readonly RenderOperations ShadowRenderOperations = new RenderOperations();

        private BoundingSphere BoundingSphere = new BoundingSphere();

        public DeferredRenderer(Common.ResourceManager resourceManager, Backend backend, int width, int height)
        {
            Settings.ShadowQuality = ShadowQuality.High;
            Settings.EnableShadows = true;
            Settings.ShadowRenderDistance = 128.0f;

            ResourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
            Backend = backend ?? throw new ArgumentNullException("backend");

            ScreenSize = new Vector2(width, height);

            GBuffer = Backend.CreateRenderTarget("gbuffer", new Definition(width, height, true, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba8, Renderer.PixelType.UnsignedByte, 0),
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 1),
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba8, Renderer.PixelType.UnsignedByte, 2),
                new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent24, Renderer.PixelType.Float, 0)
            }));

            LightAccumulation = Backend.CreateRenderTarget("light_accumulation", new Definition(width, height, false, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
                new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent24, Renderer.PixelType.Float, 0)
            }));

            Temporary = Backend.CreateRenderTarget("deferred_temp", new Definition(width, height, false, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0)
            }));

            Output = Backend.CreateRenderTarget("deferred_output", new Definition(width, height, false, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0)
            }));

            SpotShadowsRenderTarget = Backend.CreateRenderTarget("spot_shadows", new Definition(512, 512, true, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
            }));

            PointShadowsRenderTarget = Backend.CreateRenderTarget("point_shadows", new Definition(512, 512, true, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
            }, true));

            int cascadeResolution = 2048;
            DirectionalShadowsRenderTarget = new RenderTarget[]
            {
                Backend.CreateRenderTarget("directional_shadows_csm0", new Definition(cascadeResolution, cascadeResolution, true, new List<Definition.Attachment>()
                {
                    new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
                })),
                Backend.CreateRenderTarget("directional_shadows_csm1", new Definition(cascadeResolution, cascadeResolution, true, new List<Definition.Attachment>()
                {
                    new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
                })),
                Backend.CreateRenderTarget("directional_shadows_csm2", new Definition(cascadeResolution, cascadeResolution, true, new List<Definition.Attachment>()
                {
                    new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
                }))
            };

            AmbientLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("/shaders/deferred/ambient");

            // Init light shaders
            var lightTypes = new string[] { "DIRECTIONAL_LIGHT", "POINT_LIGHT", "SPOT_LIGHT" };
            var lightPermutations = new string[] { "NO_SHADOWS", "SHADOWS;SHADOW_QUALITY_LOWEST", "SHADOWS;SHADOW_QUALITY_LOW", "SHADOWS;SHADOW_QUALITY_MEDIUM", "SHADOWS;SHADOW_QUALITY_HIGH" };

            LightShaders = new Resources.ShaderProgram[lightTypes.Length * lightPermutations.Length];
            LightParams = new LightParams[lightTypes.Length * lightPermutations.Length];

            for (var l = 0; l < lightTypes.Length; l++)
            {
                var lightType = lightTypes[l];
                for (var p = 0; p < lightPermutations.Length; p++)
                {
                    var index = l * lightPermutations.Length + p;
                    var defines = lightType + ";" + lightPermutations[p];

                    if (lightType == "POINT_LIGHT")
                        defines += ";SHADOWS_CUBE";

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
            RenderShadowsSkinnedCubeShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows_cube", "SKINNED");
            FogShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/fog");
            _lightComputeShader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/light_cs");
            RandomNoiseTexture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("/textures/random_n");

            QuadMesh = Backend.CreateBatchBuffer();
            QuadMesh.Begin();
            QuadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
            QuadMesh.End();

            UnitSphere = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("/models/unit_sphere");
            UnitCone = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("/models/unit_cone");

            AmbientRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
            LightAccumulatinRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
            ShadowsRenderState = Backend.CreateRenderState(false, true, true);
            DirectionalRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);
            LightInsideRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Triton.Renderer.CullFaceMode.Front, true, Renderer.DepthFunction.Gequal);
            LightOutsideRenderState = Backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);

            ShadowSampler = Backend.RenderSystem.CreateSampler(new Dictionary<Renderer.SamplerParameterName, int>
            {
                { SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
                { SamplerParameterName.TextureMagFilter, (int)TextureMinFilter.Linear },
                { SamplerParameterName.TextureCompareFunc, (int)DepthFunction.Lequal },
                { SamplerParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRToTexture },
                { SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
                { SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
            });

            _pointLightDataCSBuffer = Backend.RenderSystem.CreateBuffer(BufferTarget.ShaderStorageBuffer, true);
            Backend.RenderSystem.SetBufferData(_pointLightDataCSBuffer, _pointLightDataCS, true, true);
        }

        public void InitializeHandles()
        {
            CombineShader.BindUniformLocations(CombineParams);
            AmbientLightShader.BindUniformLocations(AmbientLightParams);
            for (var i = 0; i < LightParams.Length; i++)
            {
                LightShaders[i].BindUniformLocations(LightParams[i]);
            }
            RenderShadowsShader.BindUniformLocations(RenderShadowsParams);
            RenderShadowsSkinnedShader.BindUniformLocations(RenderShadowsSkinnedParams);
            RenderShadowsCubeShader.BindUniformLocations(RenderShadowsCubeParams);
            RenderShadowsSkinnedCubeShader.BindUniformLocations(RenderShadowsSkinnedCubeParams);
            FogShader.BindUniformLocations(FogParams);
            _lightComputeShader.BindUniformLocations(_computeLightParams);
        }

        public RenderTarget Render(Stage stage, Camera camera)
        {
            if (!HandlesInitialized)
            {
                InitializeHandles();
                HandlesInitialized = true;
            }

            RenderedLights = 0;

            // Init common matrices
            Matrix4 view, projection;
            camera.GetViewMatrix(out view);
            camera.GetProjectionMatrix(out projection);

            // Render scene to GBuffer
            var clearColor = stage.ClearColor;
            clearColor.W = 0;
            Backend.ProfileBeginSection(Profiler.GBuffer);
            Backend.BeginPass(GBuffer, clearColor, ClearFlags.All);
            RenderScene(stage, camera, ref view, ref projection);
            Backend.EndPass();
            Backend.ProfileEndSection(Profiler.GBuffer);

            // Render light accumulation
            Backend.ProfileBeginSection(Profiler.Lighting);
            Backend.BeginPass(LightAccumulation, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), ClearFlags.All);

            RenderAmbientLight(stage);
            RenderLights(camera, ref view, ref projection, stage.GetLights(), stage);

            Backend.EndPass();
            Backend.ProfileEndSection(Profiler.Lighting);

            var currentRenderTarget = Temporary;
            var currentSource = LightAccumulation;

            if (FogSettings.Enable)
            {
                Backend.BeginPass(currentRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), ClearFlags.Color);

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

            Backend.BeginPass(Output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), ClearFlags.Color);

            Backend.BeginInstance(CombineShader.Handle, new int[] { currentSource.Textures[0].Handle }, new int[] { Backend.DefaultSamplerNoFiltering }, LightAccumulatinRenderState);
            Backend.BindShaderVariable(CombineParams.SamplerLight, 0);
            Backend.DrawMesh(QuadMesh.MeshHandle);

            Backend.EndPass();

            return Output;
        }

        private int DispatchSize(int tgSize, int numElements)
        {
            var dispatchSize = numElements / tgSize;
            dispatchSize += numElements % tgSize > 0 ? 1 : 0;
            return dispatchSize;
        }

        private void RenderScene(Stage stage, Camera camera, ref Matrix4 view, ref Matrix4 projection)
        {
            var viewProjection = view * projection;

            RenderOperations.Reset();
            stage.PrepareRenderOperations(viewProjection, RenderOperations);

            RenderOperation[] operations;
            int count;
            RenderOperations.GetOperations(out operations, out count);

            Resources.Material activeMaterial = null;
            Matrix4 worldView, world, itWorld, worldViewProjection;

            for (var i = 0; i < count; i++)
            {
                world = operations[i].WorldMatrix;

                Matrix4.Mult(ref world, ref viewProjection, out worldViewProjection);
                Matrix4.Mult(ref world, ref view, out worldView);

                itWorld = Matrix4.Transpose(Matrix4.Invert(world));

                if (activeMaterial == null || activeMaterial.Id != operations[i].Material.Id)
                {
                    operations[i].Material.BeginInstance(Backend, camera, 0);
                }

                operations[i].Material.BindPerObject(Backend, ref world, ref worldView, ref itWorld, ref worldViewProjection, operations[i].Skeleton);
                Backend.DrawMesh(operations[i].MeshHandle);
            }
        }

        private void RenderAmbientLight(Stage stage)
        {
            Matrix4 modelViewProjection = Matrix4.Identity;

            var ambientColor = new Vector3((float)System.Math.Pow(stage.AmbientColor.X, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Y, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Z, 2.2f));

            Backend.BeginInstance(AmbientLightShader.Handle,
                new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle },
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
            var frustum = camera.GetFrustum();

            RenderPointLights(camera, frustum, ref view, ref projection, stage, lights);

            foreach (var light in lights)
            {
                if (!light.Enabled)
                    continue;

                RenderLight(camera, frustum, ref view, ref projection, stage, ref modelViewProjection, light);
            }
        }

        private void RenderLight(Camera camera, BoundingFrustum cameraFrustum, ref Matrix4 view, ref Matrix4 projection, Stage stage, ref Matrix4 modelViewProjection, Light light)
        {
            // Pad the radius of the rendered sphere a little, it's quite low poly so there will be minor artifacts otherwise
            var radius = light.Range * 1.1f;

            // Culling
            if (light.Type == LighType.PointLight)
            {
                if (!light.CastShadows)
                    return; // Handled by tiled renderer

                BoundingSphere.Center = light.Position;
                BoundingSphere.Radius = radius;

                if (!cameraFrustum.Intersects(BoundingSphere))
                    return;
            }

            RenderedLights++;

            var renderStateId = DirectionalRenderState;

            var cameraDistanceToLight = light.Position - camera.Position;

            var castShadows = light.CastShadows && Settings.EnableShadows;

            if (light.Type == LighType.PointLight || light.Type == LighType.SpotLight)
            {
                if (Vector3.DistanceSquared(light.Position, camera.Position) > Settings.ShadowRenderDistance * Settings.ShadowRenderDistance)
                {
                    castShadows = false;
                }
            }

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

            Matrix4[] shadowViewProjections = null;
            Vector4 clipDistances = Vector4.Zero;

            if (castShadows)
            {
                if (light.Type == LighType.PointLight)
                {
                    RenderShadowsCube(PointShadowsRenderTarget, light, stage, camera, out shadowViewProjection, out shadowCameraClipPlane);
                }
                else if (light.Type == LighType.SpotLight)
                {
                    RenderShadows(SpotShadowsRenderTarget, light, stage, camera, 0, out shadowViewProjection, out shadowCameraClipPlane);
                }
                else
                {
                    // Just to get rid of the unassigend error
                    shadowViewProjection = Matrix4.Identity;
                    shadowCameraClipPlane = Vector2.Zero;

                    // Do the csm
                    shadowViewProjections = new Matrix4[3];

                    float cameraNear = camera.NearClipDistance;
                    float cameraFar = camera.FarClipDistance;
                    float dist = cameraFar - cameraNear;

                    clipDistances.X = cameraNear;
                    clipDistances.Y = cameraFar * 0.1f;
                    clipDistances.Z = cameraFar * 0.4f;
                    clipDistances.W = cameraFar;

                    // Cascade 1
                    camera.NearClipDistance = clipDistances.X;
                    camera.FarClipDistance = clipDistances.Y;
                    RenderShadows(DirectionalShadowsRenderTarget[0], light, stage, camera, 0, out shadowViewProjections[0], out shadowCameraClipPlane);

                    // Cascade 2
                    camera.NearClipDistance = clipDistances.X;
                    camera.FarClipDistance = clipDistances.Z;
                    RenderShadows(DirectionalShadowsRenderTarget[1], light, stage, camera, 1, out shadowViewProjections[1], out shadowCameraClipPlane);

                    // Cascade 3
                    camera.NearClipDistance = clipDistances.X;
                    camera.FarClipDistance = clipDistances.W;
                    RenderShadows(DirectionalShadowsRenderTarget[2], light, stage, camera, 2, out shadowViewProjections[2], out shadowCameraClipPlane);

                    // Restore clip plane
                    camera.NearClipDistance = cameraNear;
                    camera.FarClipDistance = cameraFar;
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

            if (castShadows)
                lightTypeOffset += 1 + (int)Settings.ShadowQuality;

            var shader = LightShaders[lightTypeOffset];
            var shaderParams = LightParams[lightTypeOffset];

            // Setup textures and begin rendering with the chosen shader
            int[] textures;
            int[] samplers;
            if (castShadows)
            {
                if (light.Type == LighType.Directional)
                {
                    textures = new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, DirectionalShadowsRenderTarget[0].Textures[0].Handle, DirectionalShadowsRenderTarget[1].Textures[0].Handle, DirectionalShadowsRenderTarget[2].Textures[0].Handle };
                    samplers = new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, ShadowSampler, ShadowSampler, ShadowSampler };
                }
                else
                {
                    var shadowMapHandle = light.Type == LighType.PointLight ?
                    PointShadowsRenderTarget.Textures[0].Handle : SpotShadowsRenderTarget.Textures[0].Handle;

                    textures = new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, shadowMapHandle };
                    samplers = new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, ShadowSampler };
                }
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
            Backend.BindShaderVariable(shaderParams.samplerDepth, 3);

            // Common uniforms
            Backend.BindShaderVariable(shaderParams.ScreenSize, ref ScreenSize);
            Backend.BindShaderVariable(shaderParams.ModelViewProjection, ref modelViewProjection);
            Backend.BindShaderVariable(shaderParams.LightColor, ref lightColor);
            Backend.BindShaderVariable(shaderParams.CameraPosition, ref camera.Position);

            if (light.Type == LighType.Directional || light.Type == LighType.SpotLight)
            {
                var lightDirWS = light.Direction.Normalize();

                Backend.BindShaderVariable(shaderParams.LightDirection, ref lightDirWS);
            }

            if (light.Type == LighType.PointLight || light.Type == LighType.SpotLight)
            {
                Backend.BindShaderVariable(shaderParams.LightPosition, ref light.Position);
                Backend.BindShaderVariable(shaderParams.LightRange, light.Range);
                Backend.BindShaderVariable(shaderParams.LightInvSquareRadius, 1.0f / (light.Range * light.Range));
            }

            if (light.Type == LighType.SpotLight)
            {
                var spotParams = new Vector2((float)System.Math.Cos(light.InnerAngle / 2.0f), (float)System.Math.Cos(light.OuterAngle / 2.0f));
                Backend.BindShaderVariable(shaderParams.SpotParams, ref spotParams);
            }

            var inverseViewProjectionMatrix = Matrix4.Invert(view * projection);
            Backend.BindShaderVariable(shaderParams.InvViewProjection, ref inverseViewProjectionMatrix);

            if (castShadows)
            {
                Backend.BindShaderVariable(shaderParams.ClipPlane, ref shadowCameraClipPlane);
                Backend.BindShaderVariable(shaderParams.ShadowBias, light.ShadowBias);

                var texelSize = 1.0f / (light.Type == LighType.Directional ? DirectionalShadowsRenderTarget[0].Width : SpotShadowsRenderTarget.Width);
                Backend.BindShaderVariable(shaderParams.TexelSize, texelSize);

                if (light.Type == LighType.PointLight)
                {
                    Backend.BindShaderVariable(shaderParams.SamplerShadowCube, 4);
                }
                else if (light.Type == LighType.Directional)
                {
                    Backend.BindShaderVariable(shaderParams.SamplerShadow1, 4);
                    Backend.BindShaderVariable(shaderParams.SamplerShadow2, 5);
                    Backend.BindShaderVariable(shaderParams.SamplerShadow3, 6);

                    Backend.BindShaderVariable(shaderParams.ShadowViewProj1, ref shadowViewProjections[0]);
                    Backend.BindShaderVariable(shaderParams.ShadowViewProj2, ref shadowViewProjections[1]);
                    Backend.BindShaderVariable(shaderParams.ShadowViewProj3, ref shadowViewProjections[2]);
                    Backend.BindShaderVariable(shaderParams.ClipDistances, ref clipDistances);
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

        private unsafe void RenderPointLights(Camera camera, BoundingFrustum cameraFrustum, ref Matrix4 view, ref Matrix4 projection, Stage stage, IReadOnlyCollection<Light> lights)
        {
            var index = 0;

            var boundingSphere = new BoundingSphere();
            foreach (var light in lights)
            {
                if (!light.Enabled || light.CastShadows || light.Type != LighType.PointLight)
                    continue;

                var radius = light.Range * 1.1f;

                boundingSphere.Center = light.Position;
                boundingSphere.Radius = radius;

                if (!cameraFrustum.Intersects(boundingSphere))
                    continue;

                RenderedLights++;

                var lightColor = light.Color * light.Intensity;
                lightColor = new Vector3((float)System.Math.Pow(lightColor.X, 2.2f), (float)System.Math.Pow(lightColor.Y, 2.2f), (float)System.Math.Pow(lightColor.Z, 2.2f));

                _pointLightDataCS[index].LightPositionRange = new Vector4(light.Position, light.Range);
                _pointLightDataCS[index].LightColor = new Vector4(lightColor, light.Intensity);

                index++;
            }

            if (index == 0)
                return;

            fixed (PointLightDataCS* data = _pointLightDataCS)
            {
                Backend.UpdateBufferInline(_pointLightDataCSBuffer, index * sizeof(PointLightDataCS), (byte*)data);
            }
            Backend.BindBufferBase(0, _pointLightDataCSBuffer);

            var lightCount = index;
            var lightTileSize = 16;

            Backend.BeginInstance(_lightComputeShader.Handle, new int[] { GBuffer.Textures[3].Handle }, new int[] { Backend.DefaultSamplerNoFiltering }, LightAccumulatinRenderState);

            var numTilesX = (uint)DispatchSize(lightTileSize, GBuffer.Textures[0].Width);
            var numTilesY = (uint)DispatchSize(lightTileSize, GBuffer.Textures[0].Height);

            Backend.BindShaderVariable(_computeLightParams.DisplaySize, (uint)ScreenSize.X, (uint)ScreenSize.Y);
            Backend.BindShaderVariable(_computeLightParams.NumTiles, numTilesX, numTilesY);
            Backend.BindShaderVariable(_computeLightParams.NumLights, lightCount);

            var clipDistance = new Vector2(camera.NearClipDistance, camera.FarClipDistance);
            Backend.BindShaderVariable(_computeLightParams.CameraClipPlanes, ref clipDistance);

            Backend.BindShaderVariable(_computeLightParams.CameraPositionWS, ref camera.Position);
            Backend.BindShaderVariable(_computeLightParams.View, ref view);
            Backend.BindShaderVariable(_computeLightParams.Projection, ref projection);

            var inverseViewProjectionMatrix = Matrix4.Invert(view * projection);
            Backend.BindShaderVariable(_computeLightParams.InvViewProjection, ref inverseViewProjectionMatrix);
            Backend.BindImageTexture(0, GBuffer.Textures[0].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadOnly, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba8);
            Backend.BindImageTexture(1, GBuffer.Textures[1].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadOnly, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba16f);
            Backend.BindImageTexture(2, GBuffer.Textures[2].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadOnly, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba8);
            Backend.BindImageTexture(3, LightAccumulation.Textures[0].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadWrite, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba16f);

            Backend.Barrier(OpenTK.Graphics.OpenGL.MemoryBarrierFlags.AllBarrierBits);
            Backend.DispatchCompute((int)numTilesX, (int)numTilesY, 1);
            Backend.Barrier(OpenTK.Graphics.OpenGL.MemoryBarrierFlags.AllBarrierBits);
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

        private void RenderShadows(RenderTarget renderTarget, Light light, Stage stage, Camera camera, int cascadeIndex, out Matrix4 viewProjection, out Vector2 clipPlane)
        {
            Backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 1), ClearFlags.All);

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
                clipPlane.X = -boxSize.Z;
                clipPlane.Y = boxSize.Z;
            }
            else
            {
                view = Matrix4.LookAt(light.Position, light.Position + light.Direction, Vector3.UnitY);
                projection = Matrix4.CreatePerspectiveFieldOfView(light.OuterAngle, renderTarget.Width / (float)renderTarget.Height, clipPlane.X, clipPlane.Y);
            }

            viewProjection = view * projection;

            ShadowRenderOperations.Reset();
            stage.PrepareRenderOperations(view, ShadowRenderOperations, true, false);

            RenderOperation[] operations;
            int count;
            ShadowRenderOperations.GetOperations(out operations, out count);

            for (var i = 0; i < count; i++)
            {
                var world = operations[i].WorldMatrix;

                modelViewProjection = world * view * projection;

                Resources.ShaderProgram program;
                RenderShadowsParams shadowParams;

                if (operations[i].Skeleton != null)
                {
                    program = RenderShadowsSkinnedShader;
                    shadowParams = RenderShadowsSkinnedParams;
                }
                else
                {
                    program = RenderShadowsShader;
                    shadowParams = RenderShadowsParams;
                }

                Backend.BeginInstance(program.Handle, null, null, ShadowsRenderState);
                Backend.BindShaderVariable(shadowParams.ModelViewProjection, ref modelViewProjection);
                Backend.BindShaderVariable(shadowParams.ClipPlane, ref clipPlane);

                if (light.Type == LighType.Directional)
                {
                    float shadowBias = 0.05f * (cascadeIndex + 1);
                    Backend.BindShaderVariable(shadowParams.ShadowBias, shadowBias);
                }

                if (operations[i].Skeleton != null)
                {
                    Backend.BindShaderVariable(shadowParams.Bones, ref operations[i].Skeleton.FinalBoneTransforms);
                }

                Backend.DrawMesh(operations[i].MeshHandle);

                Backend.EndInstance();
            }

            Backend.EndPass();
        }

        private void RenderShadowsCube(RenderTarget renderTarget, Light light, Stage stage, Camera camera, out Matrix4 viewProjection, out Vector2 clipPlane)
        {
            Backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 1), ClearFlags.All);

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

            ShadowRenderOperations.Reset();
            stage.PrepareRenderOperations(light.Position, light.Range * 2, ShadowRenderOperations, true);

            RenderOperation[] operations;
            int count;
            ShadowRenderOperations.GetOperations(out operations, out count);

            for (var i = 0; i < count; i++)
            {
                var world = operations[i].WorldMatrix;

                Resources.ShaderProgram program;
                RenderShadowsParams shadowParams;

                if (operations[i].Skeleton != null)
                {
                    program = RenderShadowsSkinnedCubeShader;
                    shadowParams = RenderShadowsSkinnedCubeParams;
                }
                else
                {
                    program = RenderShadowsCubeShader;
                    shadowParams = RenderShadowsCubeParams;
                }

                Backend.BeginInstance(program.Handle, null, null, ShadowsRenderState);
                Backend.BindShaderVariable(shadowParams.Model, ref world);
                Backend.BindShaderVariable(shadowParams.ClipPlane, ref clipPlane);
                Backend.BindShaderVariable(shadowParams.ViewProjectionMatrices, ref viewProjectionMatrices);
                Backend.BindShaderVariable(shadowParams.LightPosition, ref light.Position);

                if (operations[i].Skeleton != null)
                {
                    Backend.BindShaderVariable(shadowParams.Bones, ref operations[i].Skeleton.FinalBoneTransforms);
                }

                Backend.DrawMesh(operations[i].MeshHandle);

                Backend.EndInstance();
            }
            Backend.EndPass();
        }

        public struct RenderSettings
        {
            public ShadowQuality ShadowQuality;
            public bool EnableShadows;
            public float ShadowRenderDistance;
        }

        private struct PointLightDataCS
        {
            public Vector4 LightPositionRange;
            public Vector4 LightColor;
        }
    }
}
