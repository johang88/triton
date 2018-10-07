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
        private readonly Common.ResourceManager _resourceManager;
        private readonly Backend _backend;

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
        private Resources.ShaderProgram _lightComputeShader;

        private const int NumLightInstances = 2048;
        private readonly PointLightDataCS[] _pointLightDataCS = new PointLightDataCS[NumLightInstances];
        private int _pointLightDataCSBuffer;

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

        public DeferredRenderer(Common.ResourceManager resourceManager, Backend backend, int width, int height)
        {
            Settings.ShadowQuality = ShadowQuality.High;
            Settings.EnableShadows = true;
            Settings.ShadowRenderDistance = 128.0f;

            _resourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
            _backend = backend ?? throw new ArgumentNullException("backend");

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
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
                //new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent24, Renderer.PixelType.Float, 0)
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

            _lightComputeShader = _resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/light_cs");

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
        }

        public void InitializeHandles()
        {
            _ambientLightShader.BindUniformLocations(_ambientLightParams);
            for (var i = 0; i < _lightParams.Length; i++)
            {
                _lightShaders[i].BindUniformLocations(_lightParams[i]);
            }
            _lightComputeShader.BindUniformLocations(_computeLightParams);
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
            RenderAmbientLight(stage);

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

        private void RenderAmbientLight(Stage stage)
        {
            Matrix4 modelViewProjection = Matrix4.Identity;

            var ambientColor = new Vector3((float)System.Math.Pow(stage.AmbientColor.X, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Y, 2.2f), (float)System.Math.Pow(stage.AmbientColor.Z, 2.2f));
            _backend.BeginInstance(_ambientLightShader.Handle,
                new int[] { _gbuffer.Textures[0].Handle, _gbuffer.Textures[1].Handle },
                new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering },
                _ambientRenderState);
            _backend.BindShaderVariable(_ambientLightParams.SamplerGBuffer0, 0);
            _backend.BindShaderVariable(_ambientLightParams.SamplerGBuffer1, 1);
            _backend.BindShaderVariable(_ambientLightParams.SamplerGBuffer3, 2);
            _backend.BindShaderVariable(_ambientLightParams.SamplerGBuffer4, 3);
            _backend.BindShaderVariable(_ambientLightParams.ModelViewProjection, ref modelViewProjection);
            _backend.BindShaderVariable(_ambientLightParams.AmbientColor, ref ambientColor);

            _backend.DrawMesh(_quadMesh.MeshHandle);
        }

        private void RenderLights(Camera camera, ref Matrix4 view, ref Matrix4 projection, IReadOnlyCollection<Light> lights, Stage stage)
        {
            var frustum = camera.GetFrustum();

            RenderPointLights(camera, frustum, ref view, ref projection, stage, lights);
            RenderSpotLights(camera, frustum, ref view, ref projection, stage, lights);

            foreach (var light in lights)
            {
                if (!light.Enabled || light.Type != LighType.Directional)
                    continue;

                RenderDirectionalLight(camera, frustum, ref view, ref projection, stage, light);
            }
        }

        private void RenderDirectionalLight(Camera camera, BoundingFrustum cameraFrustum, ref Matrix4 view, ref Matrix4 projection, Stage stage, Light light)
        {
            if (light.Type != LighType.Directional)
                return;

            RenderedLights++;

            var renderStateId = _directionalRenderState;
            var cameraDistanceToLight = light.Position - camera.Position;

            var viewProjection = view * projection;
            var modelViewProjection = viewProjection; // It's a directional light ...

            // Convert light color to linear space
            var lightColor = light.Color * light.Intensity;
            lightColor = new Vector3((float)System.Math.Pow(lightColor.X, 2.2f), (float)System.Math.Pow(lightColor.Y, 2.2f), (float)System.Math.Pow(lightColor.Z, 2.2f));

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

            var lightDirWS = light.Direction.Normalize();
            _backend.BindShaderVariable(shaderParams.LightDirection, ref lightDirWS);

            var inverseViewProjectionMatrix = Matrix4.Invert(view * projection);
            _backend.BindShaderVariable(shaderParams.InvViewProjection, ref inverseViewProjectionMatrix);

            _backend.DrawMesh(_quadMesh.MeshHandle);

            _backend.EndInstance();
        }

        private unsafe void RenderPointLights(Camera camera, BoundingFrustum cameraFrustum, ref Matrix4 view, ref Matrix4 projection, Stage stage, IReadOnlyCollection<Light> lights)
        {
            var index = 0;

            var boundingSphere = new BoundingSphere();
            foreach (var light in lights)
            {
                if (!light.Enabled || light.Type != LighType.PointLight)
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
                _backend.UpdateBufferInline(_pointLightDataCSBuffer, index * sizeof(PointLightDataCS), (byte*)data);
            }
            _backend.BindBufferBase(0, _pointLightDataCSBuffer);

            var lightCount = index;
            var lightTileSize = 16;

            _backend.BeginInstance(_lightComputeShader.Handle, new int[] { _gbuffer.Textures[3].Handle, _lightAccumulationTarget.Textures[0].Handle }, new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering }, _lightAccumulatinRenderState);

            var numTilesX = (uint)DispatchSize(lightTileSize, _gbuffer.Textures[0].Width);
            var numTilesY = (uint)DispatchSize(lightTileSize, _gbuffer.Textures[0].Height);

            _backend.BindShaderVariable(_computeLightParams.DisplaySize, (uint)_screenSize.X, (uint)_screenSize.Y);
            _backend.BindShaderVariable(_computeLightParams.NumTiles, numTilesX, numTilesY);
            _backend.BindShaderVariable(_computeLightParams.NumLights, lightCount);

            var clipDistance = new Vector2(camera.NearClipDistance, camera.FarClipDistance);
            _backend.BindShaderVariable(_computeLightParams.CameraClipPlanes, ref clipDistance);

            _backend.BindShaderVariable(_computeLightParams.CameraPositionWS, ref camera.Position);
            _backend.BindShaderVariable(_computeLightParams.View, ref view);
            _backend.BindShaderVariable(_computeLightParams.Projection, ref projection);

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

        private unsafe void RenderSpotLights(Camera camera, BoundingFrustum cameraFrustum, ref Matrix4 view, ref Matrix4 projection, Stage stage, IReadOnlyCollection<Light> lights)
        {
            foreach (var light in lights)
            {
                if (!light.Enabled || light.Type != LighType.SpotLight)
                    continue;

                // TODO!
            }
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
