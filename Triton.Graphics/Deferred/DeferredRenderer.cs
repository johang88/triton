using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Resources;
using Triton.Renderer;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Deferred
{
    public class DeferredRenderer
    {
        private const int SpotShadowAtlasResolution = 2048;
        private const int SpotShadowResolution = 256;
        private const int MaxShadowCastingSpotLights = SpotShadowAtlasResolution / SpotShadowResolution;

        private const int MaxShadowCastingPointLights = 5;
        private const int PointShadowResolution = 256;
        private const int PointShadowAtlasResolution = PointShadowResolution * MaxShadowCastingPointLights * 6;

        private readonly Triton.Resources.ResourceManager _resourceManager;
        private readonly Backend _backend;
        private readonly ShadowRenderer _shadowRenderer;

        private AmbientLightParams _ambientLightParams = new AmbientLightParams();
        private LightParams[] _lightParams;
        private CombineParams _combineParams = new CombineParams();
        private LightParams _computeLightParams = new LightParams();

        private Vector2 _screenSize;

        private readonly RenderTarget _gbuffer;
        private readonly RenderTarget _lightAccumulationTarget;

        private BatchBuffer _quadMesh;

        private Resources.ShaderProgram _ambientLightShader;
        private Resources.ShaderProgram[] _lightShaders;
        private Resources.ShaderProgram[] _lightComputeShader = new ShaderProgram[(int)ShadowQuality.High + 1];

        private const int NumLightInstances = 1024;
        private readonly PointLightDataCS[] _pointLightDataCS = new PointLightDataCS[NumLightInstances];
        private int _pointLightDataCSBuffer;

        private readonly SpotLightDataCS[] _spotLightDataCS = new SpotLightDataCS[NumLightInstances];
        private int _spotLightDataCSBuffer;

        private readonly int[] _lightToShadowIndex = new int[NumLightInstances + NumLightInstances];
        private int _lightToShadowIndexCSBuffer;

        private bool _initialized = false;

        private int _ambientRenderState;
        private int _lightAccumulatinRenderState;

        private int _directionalRenderState;
        private int _lightInsideRenderState;
        private int _lightOutsideRenderState;

        public int _directionalShaderOffset = 0;

        public int RenderedLights = 0;

        public RenderSettings Settings;

        private readonly RenderOperations _renderOperations = new RenderOperations();

        private int[] _lightTextureBinds = new int[5];
        private int[] _lightSamplers = new int[5];

        private RenderTarget _shadowBuffer = null;
        private Texture _specularIntegarion;

        public RenderTarget SpotShadowAtlas;
        private int _spotShadowCount = 0;
        private Matrix4[] _spotShadowMatrices = new Matrix4[MaxShadowCastingSpotLights];

        public RenderTarget PointShadowAtlas;
        private int _pointShadowCount = 0;
        private Matrix4[] _pointShadowMatrices = new Matrix4[MaxShadowCastingPointLights * 6];

        public DeferredRenderer(Triton.Resources.ResourceManager resourceManager, Backend backend, ShadowRenderer shadowRenderer, int width, int height)
        {
            Settings.ShadowQuality = ShadowQuality.High;
            Settings.EnableShadows = true;
            Settings.ShadowRenderDistance = 128.0f;

            _resourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
            _backend = backend ?? throw new ArgumentNullException("backend");
            _shadowRenderer = shadowRenderer ?? throw new ArgumentNullException(nameof(shadowRenderer));

            _screenSize = new Vector2(width, height);

            _gbuffer = _backend.CreateRenderTarget("gbuffer", new Definition(width, height, true, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba8, Renderer.PixelType.UnsignedByte, 0),
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 1),
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba8, Renderer.PixelType.UnsignedByte, 2),
                new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent32f, Renderer.PixelType.Float, 0)
            }));

            _lightAccumulationTarget = _backend.CreateRenderTarget("light_accumulation", new Definition(width, height, false, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0)
            }));

            SpotShadowAtlas = _backend.CreateRenderTarget("spot_shadow_atlas", new Definition(SpotShadowAtlasResolution, SpotShadowResolution, true, new List<Definition.Attachment>()
                    {
						new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0)
                    }));

            PointShadowAtlas = _backend.CreateRenderTarget("point_shadow_atlas", new Definition(PointShadowAtlasResolution, PointShadowResolution, true, new List<Definition.Attachment>()
                    {
						new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0)
                    }));

            _ambientLightShader = _resourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("/shaders/deferred/ambient");

            // Init light shaders
            var lightTypes = new string[] { "DIRECTIONAL_LIGHT" };
            var lightPermutations = new string[] { "NO_SHADOWS", "SHADOWS" };

            _lightShaders = new Resources.ShaderProgram[lightTypes.Length * lightPermutations.Length];
            _lightParams = new LightParams[lightTypes.Length * lightPermutations.Length];

            for (var l = 0; l < lightTypes.Length; l++)
            {
                var lightType = lightTypes[l];
                for (var p = 0; p < lightPermutations.Length; p++)
                {
                    var index = l * lightPermutations.Length + p;
                    var defines = lightType + ";" + lightPermutations[p];

                    _lightShaders[index] = _resourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("/shaders/deferred/light", defines);
                    _lightParams[index] = new LightParams();
                }
            }

            _directionalShaderOffset = 0;

            var shadowQualities = new string[] { "SHADOW_QUALITY_LOWEST", "SHADOW_QUALITY_LOW", "SHADOW_QUALITY_MEDIUM", "SHADOW_QUALITY_HIGH" };
            for (var i = 0; i < _lightComputeShader.Length; i++)
            {
                _lightComputeShader[i] = _resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/light_cs", shadowQualities[i]);
            }
            
            _specularIntegarion = _resourceManager.Load<Resources.Texture>("/textures/specular_integration");

            _quadMesh = _backend.CreateBatchBuffer();
            _quadMesh.Begin();
            _quadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
            _quadMesh.End();

            _ambientRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
            _lightAccumulatinRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
            _directionalRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);
            _lightInsideRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Triton.Renderer.CullFaceMode.Front, true, Renderer.DepthFunction.Gequal);
            _lightOutsideRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);

            _pointLightDataCSBuffer = _backend.RenderSystem.CreateBuffer(BufferTarget.ShaderStorageBuffer, true);
            _backend.RenderSystem.SetBufferData(_pointLightDataCSBuffer, _pointLightDataCS, true, true);

            _spotLightDataCSBuffer = _backend.RenderSystem.CreateBuffer(BufferTarget.ShaderStorageBuffer, true);
            _backend.RenderSystem.SetBufferData(_spotLightDataCSBuffer, _spotLightDataCS, true, true);

            _lightToShadowIndexCSBuffer = _backend.RenderSystem.CreateBuffer(BufferTarget.ShaderStorageBuffer, true);
            _backend.RenderSystem.SetBufferData(_lightToShadowIndexCSBuffer, _lightToShadowIndex, true, true);
        }

        public void InitializeHandles()
        {
            _ambientLightShader.BindUniformLocations(_ambientLightParams);
            for (var i = 0; i < _lightParams.Length; i++)
            {
                _lightShaders[i].BindUniformLocations(_lightParams[i]);
            }
            // Expect all of them to have the same uniform locations (probably not a good idea)
            _lightComputeShader[0].BindUniformLocations(_computeLightParams);
        }

        private void Initialize()
        {
            if (!_initialized)
            {
                InitializeHandles();
                _initialized = true;
            }
        }

        public RenderTarget RenderGBuffer(Stage stage, Camera camera)
        {
            Initialize();

            // Init common matrices
            camera.GetViewMatrix(out var view);
            camera.GetProjectionMatrix(out var projection);

            // Render scene to GBuffer
            var clearColor = stage.ClearColor;
            clearColor.W = 0;
            _backend.ProfileBeginSection(Profiler.GBuffer);
            _backend.BeginPass(_gbuffer, clearColor, ClearFlags.All);
            RenderScene(stage, camera, ref view, ref projection);
            _backend.EndPass();
            _backend.ProfileEndSection(Profiler.GBuffer);

            return _gbuffer;
        }

        public RenderTarget RenderLighting(Stage stage, Camera camera, RenderTarget shadowBuffer)
        {
            Initialize();

            _shadowBuffer = shadowBuffer;

            RenderedLights = 0;

            // Init common matrices
            camera.GetViewMatrix(out var view);
            camera.GetProjectionMatrix(out var projection);

            // Render light accumulation
            _backend.ProfileBeginSection(Profiler.Lighting);

            _backend.BeginPass(_lightAccumulationTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), ClearFlags.All);
            RenderLights(camera, ref view, ref projection, stage.GetLights(), stage);
            RenderAmbientLight(camera, stage);

            _backend.EndPass();
            _backend.ProfileEndSection(Profiler.Lighting);

            return _lightAccumulationTarget;
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

            _renderOperations.Reset();
            stage.PrepareRenderOperations(viewProjection, _renderOperations);

            _renderOperations.GetOperations(out var operations, out var count);

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
                    operations[i].Material.BeginInstance(_backend, camera, 0);
                }

                operations[i].Material.BindPerObject(_backend, ref world, ref worldView, ref itWorld, ref worldViewProjection, operations[i].Skeleton);
                _backend.DrawMesh(operations[i].MeshHandle);
            }
        }

        private void RenderAmbientLight(Camera camera, Stage stage)
        {
            Matrix4 modelViewProjection = Matrix4.Identity;

            var irradianceHandle = stage.AmbientLight?.Irradiance?.Handle ?? 0;
            var specularHandle = stage.AmbientLight?.Specular?.Handle ?? 0;

            camera.GetViewMatrix(out var view);
            camera.GetProjectionMatrix(out var projection);
            var inverseViewProjectionMatrix = Matrix4.Invert(view * projection);

            //var ambientColor = new Vector3((float)System.Math.Pow(stage.AmbientColor.X, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Y, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Z, 2.2f));
            var ambientColor = stage.AmbientColor;
            _backend.BeginInstance(_ambientLightShader.Handle,
                new int[] { _gbuffer.Textures[0].Handle, _gbuffer.Textures[1].Handle, _gbuffer.Textures[2].Handle, _gbuffer.Textures[3].Handle, irradianceHandle, specularHandle, _specularIntegarion.Handle },
                new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSampler, _backend.DefaultSampler, _backend.DefaultSamplerNoFiltering },
                _ambientRenderState);
            _backend.BindShaderVariable(_ambientLightParams.SamplerGBuffer0, 0);
            _backend.BindShaderVariable(_ambientLightParams.SamplerGBuffer1, 1);
            _backend.BindShaderVariable(_ambientLightParams.SamplerGBuffer2, 2);
            _backend.BindShaderVariable(_ambientLightParams.SamplerDepth, 3);
            _backend.BindShaderVariable(_ambientLightParams.SamplerIrradiance, 4);
            _backend.BindShaderVariable(_ambientLightParams.SamplerSpecular, 5);
            _backend.BindShaderVariable(_ambientLightParams.SamplerSpecularIntegration, 6);
            _backend.BindShaderVariable(_ambientLightParams.ModelViewProjection, ref modelViewProjection);
            _backend.BindShaderVariable(_ambientLightParams.AmbientColor, ref ambientColor);
            _backend.BindShaderVariable(_ambientLightParams.Mode, irradianceHandle == 0 ? 0 : 1);
            _backend.BindShaderVariable(_ambientLightParams.IrradianceStrength, stage.AmbientLight?.IrradianceStrength ?? 1.0f);
            _backend.BindShaderVariable(_ambientLightParams.SpecularStrength, stage.AmbientLight?.SpecularStrength ?? 1.0f);
            _backend.BindShaderVariable(_ambientLightParams.CameraPosition, ref camera.Position);
            _backend.BindShaderVariable(_ambientLightParams.InvViewProjection, ref inverseViewProjectionMatrix);

            _backend.DrawMesh(_quadMesh.MeshHandle);
        }

        private void RenderLights(Camera camera, ref Matrix4 view, ref Matrix4 projection, IReadOnlyCollection<Components.LightComponent> lights, Stage stage)
        {
            var frustum = camera.GetFrustum();

            RenderTiledLights(camera, frustum, ref view, ref projection, stage, lights);

            foreach (var light in lights)
            {
                if (!light.Enabled || light.Type != LighType.Directional)
                    continue;

                RenderDirectionalLight(camera, frustum, ref view, ref projection, stage, light);
            }
        }

        private void RenderDirectionalLight(Camera camera, BoundingFrustum cameraFrustum, ref Matrix4 view, ref Matrix4 projection, Stage stage, Components.LightComponent light)
        {
            if (light.Type != LighType.Directional)
                return;

            RenderedLights++;

            var renderStateId = _directionalRenderState;

            var viewProjection = view * projection;
            var modelViewProjection = viewProjection; // It's a directional light ...

            // Convert light color to linear space
            var lightColor = light.Color * light.Intensity;
            //lightColor = new Vector3((float)System.Math.Pow(lightColor.X, 2.2f), (float)System.Math.Pow(lightColor.Y, 2.2f), (float)System.Math.Pow(lightColor.Z, 2.2f)) * light.Intensity;

            // Select the correct shader
            var lightTypeOffset = _shadowBuffer != null ? 1 : 0;

            var shader = _lightShaders[lightTypeOffset];
            var shaderParams = _lightParams[lightTypeOffset];

            // Setup textures and begin rendering with the chosen shader
            _lightTextureBinds[0] = _gbuffer.Textures[0].Handle;
            _lightTextureBinds[1] = _gbuffer.Textures[1].Handle;
            _lightTextureBinds[2] = _gbuffer.Textures[2].Handle;
            _lightTextureBinds[3] = _gbuffer.Textures[3].Handle;
            if (_shadowBuffer != null)
                _lightTextureBinds[4] = _shadowBuffer.Textures[0].Handle;
            else
                _lightTextureBinds[4] = 0;

            _lightSamplers[0] = _backend.DefaultSamplerNoFiltering;
            _lightSamplers[1] = _backend.DefaultSamplerNoFiltering;
            _lightSamplers[2] = _backend.DefaultSamplerNoFiltering;
            _lightSamplers[3] = _backend.DefaultSamplerNoFiltering;
            if (light.Type == LighType.Directional && _shadowBuffer != null && light.CastShadows)
                _lightSamplers[4] = _backend.DefaultSamplerNoFiltering;
            else
                _lightSamplers[4] = 0;

            _backend.BeginInstance(shader.Handle, _lightTextureBinds, _lightSamplers, renderStateId);

            // Setup texture samplers
            _backend.BindShaderVariable(shaderParams.SamplerGBuffer0, 0);
            _backend.BindShaderVariable(shaderParams.SamplerGBuffer1, 1);
            _backend.BindShaderVariable(shaderParams.SamplerGBuffer2, 2);
            _backend.BindShaderVariable(shaderParams.SamplerDepth, 3);
            _backend.BindShaderVariable(shaderParams.SamplerShadow, 4);

            // Common uniforms
            _backend.BindShaderVariable(shaderParams.ScreenSize, ref _screenSize);
            _backend.BindShaderVariable(shaderParams.ModelViewProjection, ref modelViewProjection);
            _backend.BindShaderVariable(shaderParams.LightColor, ref lightColor);
            _backend.BindShaderVariable(shaderParams.CameraPosition, ref camera.Position);

            Vector3 unitZ = Vector3.UnitZ;
            Vector3.Transform(ref unitZ, ref light.Owner.Orientation, out var lightDirWS);
            lightDirWS = lightDirWS.Normalize();

            _backend.BindShaderVariable(shaderParams.LightDirection, ref lightDirWS);

            var inverseViewProjectionMatrix = Matrix4.Invert(view * projection);
            _backend.BindShaderVariable(shaderParams.InvViewProjection, ref inverseViewProjectionMatrix);

            _backend.DrawMesh(_quadMesh.MeshHandle);

            _backend.EndInstance();
        }

        private unsafe void RenderTiledLights(Camera camera, BoundingFrustum cameraFrustum, ref Matrix4 view, ref Matrix4 projection, Stage stage, IReadOnlyCollection<Components.LightComponent> lights)
        {
            // Do light stuff!
            var boundingSphere = new BoundingSphere();
            int pointLightCount = 0, spotLightCount = 0;

            _spotShadowCount = 0;
            _pointShadowCount = 0;

            foreach (var light in lights)
            {
                if (!light.Enabled || (light.Type != LighType.PointLight && light.Type != LighType.SpotLight))
                    continue;

                var radius = light.Range * 1.1f;

                var lightPositionWS = light.Owner.Position;

                boundingSphere.Center = lightPositionWS;
                boundingSphere.Radius = radius;

                if (!cameraFrustum.Intersects(boundingSphere))
                    continue;

                RenderedLights++;

                var lightColor = light.Color * light.Intensity;
                //lightColor = new Vector3((float)System.Math.Pow(lightColor.X, 2.2f), (float)System.Math.Pow(lightColor.Y, 2.2f), (float)System.Math.Pow(lightColor.Z, 2.2f)) * light.Intensity;

                if (light.Type == LighType.PointLight)
                {
                    _pointLightDataCS[pointLightCount].PositionRange = new Vector4(lightPositionWS, light.Range);
                    _pointLightDataCS[pointLightCount].Color = new Vector4(lightColor, light.Intensity);
                    
                    if (light.CastShadows)
                    {
                        // TODO: Render Point Light Shadows!
                        _lightToShadowIndex[pointLightCount] = RenderPointLightShadows(stage, light);
                    }
                    else
                    {
                        _lightToShadowIndex[pointLightCount] = -1;
                    }

                    pointLightCount++;
                }
                else
                {
                    Vector3 unitZ = Vector3.UnitZ;
                    Vector3.Transform(ref unitZ, ref light.Owner.Orientation, out var lightDirWS);
                    lightDirWS = lightDirWS.Normalize();

                    _spotLightDataCS[spotLightCount].PositionRange = new Vector4(lightPositionWS, light.Range);
                    _spotLightDataCS[spotLightCount].ColorInnerAngle = new Vector4(lightColor, light.InnerAngle);
                    _spotLightDataCS[spotLightCount].DirectionOuterAngle = new Vector4(lightDirWS, light.OuterAngle);
                    
                    if (light.CastShadows == true)
                    {
                        _lightToShadowIndex[NumLightInstances + spotLightCount] = RenderSpotLightShadows(stage, light);
                    }
                    else
                    {
                        _lightToShadowIndex[NumLightInstances + spotLightCount] = -1;
                    }

                    spotLightCount++;
                }
            }

            // Reset render target
            _backend.BeginPass(_lightAccumulationTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), ClearFlags.All);

            if (pointLightCount == 0 && spotLightCount == 0)
                return;

            fixed (PointLightDataCS* data = _pointLightDataCS)
            {
                _backend.UpdateBufferInline(_pointLightDataCSBuffer, pointLightCount * sizeof(PointLightDataCS), (byte*)data);
            }

            fixed (SpotLightDataCS* data = _spotLightDataCS)
            {
                _backend.UpdateBufferInline(_spotLightDataCSBuffer, spotLightCount * sizeof(SpotLightDataCS), (byte*)data);
            }

            fixed (int* data = _lightToShadowIndex)
            {
                _backend.UpdateBufferInline(_lightToShadowIndexCSBuffer, _lightToShadowIndex.Length * sizeof(int), (byte*)data);
            }

            _backend.BindBufferBase(0, _pointLightDataCSBuffer);
            _backend.BindBufferBase(1, _spotLightDataCSBuffer);
            _backend.BindBufferBase(2, _lightToShadowIndexCSBuffer);

            var lightTileSize = 16;

            _backend.BeginInstance(_lightComputeShader[(int)Settings.ShadowQuality].Handle, new int[] { _gbuffer.Textures[3].Handle, SpotShadowAtlas.Textures[0].Handle, PointShadowAtlas.Textures[0].Handle }, new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering }, _lightAccumulatinRenderState);

            var numTilesX = (uint)DispatchSize(lightTileSize, _gbuffer.Textures[0].Width);
            var numTilesY = (uint)DispatchSize(lightTileSize, _gbuffer.Textures[0].Height);

            _backend.BindShaderVariable(_computeLightParams.DisplaySize, (uint)_screenSize.X, (uint)_screenSize.Y);
            _backend.BindShaderVariable(_computeLightParams.NumTiles, numTilesX, numTilesY);
            _backend.BindShaderVariable(_computeLightParams.NumPointLights, pointLightCount);
            _backend.BindShaderVariable(_computeLightParams.NumSpotLights, spotLightCount);

            var clipDistance = new Vector2(camera.NearClipDistance, camera.FarClipDistance);
            _backend.BindShaderVariable(_computeLightParams.CameraClipPlanes, ref clipDistance);

            _backend.BindShaderVariable(_computeLightParams.CameraPositionWS, ref camera.Position);
            _backend.BindShaderVariable(_computeLightParams.View, ref view);
            _backend.BindShaderVariable(_computeLightParams.Projection, ref projection);
            _backend.BindShaderVariable(_computeLightParams.SpotShadowMatrices, ref _spotShadowMatrices);
            _backend.BindShaderVariable(_computeLightParams.PointShadowMatrices, ref _pointShadowMatrices);

            var inverseViewProjectionMatrix = Matrix4.Invert(view * projection);
            var inverseProjectionMatrix = Matrix4.Invert(projection);
            _backend.BindShaderVariable(_computeLightParams.InvViewProjection, ref inverseViewProjectionMatrix);
            _backend.BindShaderVariable(_computeLightParams.InvProjection, ref inverseProjectionMatrix);
            _backend.BindImageTexture(0, _gbuffer.Textures[0].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadOnly, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba8);
            _backend.BindImageTexture(1, _gbuffer.Textures[1].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadOnly, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba16f);
            _backend.BindImageTexture(2, _gbuffer.Textures[2].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadOnly, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba8);
            _backend.BindImageTexture(3, _lightAccumulationTarget.Textures[0].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadWrite, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba16f);

            _backend.Barrier(OpenTK.Graphics.OpenGL.MemoryBarrierFlags.AllBarrierBits);
            _backend.DispatchCompute((int)numTilesX, (int)numTilesY, 1);
            _backend.Barrier(OpenTK.Graphics.OpenGL.MemoryBarrierFlags.AllBarrierBits);
        }

        /// <summary>
        /// Render spot light shadows, returns the index of the shadow map on the atlas
        /// </summary>
        private int RenderSpotLightShadows(Stage stage, Components.LightComponent light)
        {
            if (_spotShadowCount >= MaxShadowCastingSpotLights)
            {
                return -1;
            }

            // Light direction
            Vector3 unitZ = Vector3.UnitZ;
            Vector3.Transform(ref unitZ, ref light.Owner.Orientation, out var lightDirWS);
            lightDirWS = lightDirWS.Normalize();

            // Calculate index and viewport
            var index = _spotShadowCount++;

            var x = index * SpotShadowResolution;
            var y = 0;
            
            // Camera matrix
            var view = Matrix4.LookAt(light.Owner.Position, light.Owner.Position + lightDirWS, Vector3.UnitY);
            var projection = Matrix4.CreatePerspectiveFieldOfView(light.OuterAngle, 1.0f, 0.1f, light.Range * 1.2f);

            var viewProjection = view * projection;
            _spotShadowMatrices[index] = viewProjection;

            // Render the shadow map!
            _backend.BeginPass(SpotShadowAtlas, new Vector4(0, 0, 0, 1), x, y, SpotShadowResolution, SpotShadowResolution, ClearFlags.All);
            _shadowRenderer.RenderShadowMap(light, stage, lightDirWS, view, projection);
            _backend.EndPass();

            return index;
        }

        private int RenderPointLightShadows(Stage stage, Components.LightComponent light)
        {
            if (_pointShadowCount >= MaxShadowCastingPointLights)
            {
                return -1;
            }

            // Calculate index and viewport
            var index = (_pointShadowCount++) * 6;

            var dir = new Vector3[]
            {
                new Vector3(1, 0, 0), 
                new Vector3(-1, 0, 0), 
                new Vector3(0, 1, 0),
                new Vector3(0, -1, 0),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, 1)
            };

            var up = new Vector3[]
            {
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, -1),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0)
            };

            var projection = Matrix4.CreatePerspectiveFieldOfView(Math.Util.DegreesToRadians(92.0f), 1.0f, 0.1f, light.Range);

            for (var i = 0; i < 6; i++)
            {
                var x = (index + i) * PointShadowResolution;
                var y = 0;

                // Camera matrix
                var view = Matrix4.LookAt(light.Owner.Position, light.Owner.Position + dir[i], up[i]);

                var viewProjection = view * projection;
                _pointShadowMatrices[index + i] = viewProjection;

                // Render the shadow map!
                _backend.BeginPass(PointShadowAtlas, new Vector4(0, 0, 0, 1), x, y, PointShadowResolution, PointShadowResolution, ClearFlags.All);
                _shadowRenderer.RenderShadowMap(light, stage, dir[i], view, projection);
                _backend.EndPass();
            }

            return index;
        }

        public struct RenderSettings
        {
            public ShadowQuality ShadowQuality;
            public bool EnableShadows;
            public float ShadowRenderDistance;
        }

        private struct PointLightDataCS
        {
            public Vector4 PositionRange;
            public Vector4 Color;
        }

        private struct SpotLightDataCS
        {
            public Vector4 PositionRange;
            public Vector4 ColorInnerAngle;
            public Vector4 DirectionOuterAngle;
        }
    }
}
