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
        private RenderShadowsParams _renderShadowsParams = new RenderShadowsParams();
        private RenderShadowsParams _renderShadowsCubeParams = new RenderShadowsParams();
        private RenderShadowsParams _renderShadowsSkinnedParams = new RenderShadowsParams();
        private RenderShadowsParams _renderShadowsCubeSkinnedParams = new RenderShadowsParams();
        private LightParams _computeLightParams = new LightParams();

        private Vector2 _screenSize;

        public readonly RenderTarget GBuffer;
        private readonly RenderTarget _lightAccumulationTarget;
        private readonly RenderTarget _spotShadowsRenderTarget;
        private readonly RenderTarget _pointShadowsRenderTarget;
        private readonly RenderTarget[] _directionalShadowsRenderTarget;

        private BatchBuffer _quadMesh;
        private Resources.Mesh _unitSphere;
        private Resources.Mesh _unitCone;

        private Resources.ShaderProgram _ambientLightShader;
        private Resources.ShaderProgram[] _lightShaders;
        private Resources.ShaderProgram _renderShadowsShader;
        private Resources.ShaderProgram _renderShadowsCubeShader;
        private Resources.ShaderProgram _renderShadowsSkinnedShader;
        private Resources.ShaderProgram _renderShadowsSkinnedCubeShader;
        private Resources.ShaderProgram _lightComputeShader;

        private const int NumLightInstances = 2048;
        private readonly PointLightDataCS[] _pointLightDataCS = new PointLightDataCS[NumLightInstances];
        private int _pointLightDataCSBuffer;

        // Used for point light shadows
        private Resources.Texture _randomNoiseTexture;

        private bool _initialized = false;

        private int _ambientRenderState;
        private int _lightAccumulatinRenderState;
        private int _shadowsRenderState;

        private int _directionalRenderState;
        private int _lightInsideRenderState;
        private int _lightOutsideRenderState;

        private int _shadowSampler;

        public int _directionalShaderOffset = 0;
        public int _pointLightShaderOffset = 0;
        public int _spotLightShaderOffset = 0;

        public int RenderedLights = 0;

        public RenderSettings Settings;

        private readonly RenderOperations _renderOperations = new RenderOperations();
        private readonly RenderOperations _shadowRenderOperations = new RenderOperations();

        public DeferredRenderer(Common.ResourceManager resourceManager, Backend backend, int width, int height)
        {
            Settings.ShadowQuality = ShadowQuality.High;
            Settings.EnableShadows = true;
            Settings.ShadowRenderDistance = 128.0f;

            _resourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
            _backend = backend ?? throw new ArgumentNullException("backend");

            _screenSize = new Vector2(width, height);

            GBuffer = _backend.CreateRenderTarget("gbuffer", new Definition(width, height, true, new List<Definition.Attachment>()
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

            _spotShadowsRenderTarget = _backend.CreateRenderTarget("spot_shadows", new Definition(512, 512, true, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
            }));

            _pointShadowsRenderTarget = _backend.CreateRenderTarget("point_shadows", new Definition(512, 512, true, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
            }, true));

            int cascadeResolution = 2048;
            _directionalShadowsRenderTarget = new RenderTarget[]
            {
                _backend.CreateRenderTarget("directional_shadows_csm0", new Definition(cascadeResolution, cascadeResolution, true, new List<Definition.Attachment>()
                {
                    new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
                })),
                _backend.CreateRenderTarget("directional_shadows_csm1", new Definition(cascadeResolution, cascadeResolution, true, new List<Definition.Attachment>()
                {
                    new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
                })),
                _backend.CreateRenderTarget("directional_shadows_csm2", new Definition(cascadeResolution, cascadeResolution, true, new List<Definition.Attachment>()
                {
                    new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0),
                }))
            };

            _ambientLightShader = _resourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("/shaders/deferred/ambient");

            // Init light shaders
            var lightTypes = new string[] { "DIRECTIONAL_LIGHT", "POINT_LIGHT", "SPOT_LIGHT" };
            var lightPermutations = new string[] { "NO_SHADOWS", "SHADOWS;SHADOW_QUALITY_LOWEST", "SHADOWS;SHADOW_QUALITY_LOW", "SHADOWS;SHADOW_QUALITY_MEDIUM", "SHADOWS;SHADOW_QUALITY_HIGH" };

            _lightShaders = new Resources.ShaderProgram[lightTypes.Length * lightPermutations.Length];
            _lightParams = new LightParams[lightTypes.Length * lightPermutations.Length];

            for (var l = 0; l < lightTypes.Length; l++)
            {
                var lightType = lightTypes[l];
                for (var p = 0; p < lightPermutations.Length; p++)
                {
                    var index = l * lightPermutations.Length + p;
                    var defines = lightType + ";" + lightPermutations[p];

                    if (lightType == "POINT_LIGHT")
                        defines += ";SHADOWS_CUBE";

                    _lightShaders[index] = _resourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("/shaders/deferred/light", defines);
                    _lightParams[index] = new LightParams();
                }
            }

            _directionalShaderOffset = 0;
            _pointLightShaderOffset = 1 * lightPermutations.Length;
            _spotLightShaderOffset = 2 * lightPermutations.Length;

            _renderShadowsShader = _resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows");
            _renderShadowsSkinnedShader = _resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows", "SKINNED");
            _renderShadowsCubeShader = _resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows_cube");
            _renderShadowsSkinnedCubeShader = _resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows_cube", "SKINNED");
            _lightComputeShader = _resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/light_cs");
            _randomNoiseTexture = _resourceManager.Load<Triton.Graphics.Resources.Texture>("/textures/random_n");

            _quadMesh = _backend.CreateBatchBuffer();
            _quadMesh.Begin();
            _quadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
            _quadMesh.End();

            _unitSphere = _resourceManager.Load<Triton.Graphics.Resources.Mesh>("/models/unit_sphere");
            _unitCone = _resourceManager.Load<Triton.Graphics.Resources.Mesh>("/models/unit_cone");

            _ambientRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
            _lightAccumulatinRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
            _shadowsRenderState = _backend.CreateRenderState(false, true, true);
            _directionalRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);
            _lightInsideRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Triton.Renderer.CullFaceMode.Front, true, Renderer.DepthFunction.Gequal);
            _lightOutsideRenderState = _backend.CreateRenderState(true, false, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, Renderer.CullFaceMode.Back, true, Triton.Renderer.DepthFunction.Lequal);

            _shadowSampler = _backend.RenderSystem.CreateSampler(new Dictionary<Renderer.SamplerParameterName, int>
            {
                { SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
                { SamplerParameterName.TextureMagFilter, (int)TextureMinFilter.Linear },
                { SamplerParameterName.TextureCompareFunc, (int)DepthFunction.Lequal },
                { SamplerParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRToTexture },
                { SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
                { SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
            });

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
            _renderShadowsShader.BindUniformLocations(_renderShadowsParams);
            _renderShadowsSkinnedShader.BindUniformLocations(_renderShadowsSkinnedParams);
            _renderShadowsCubeShader.BindUniformLocations(_renderShadowsCubeParams);
            _renderShadowsSkinnedCubeShader.BindUniformLocations(_renderShadowsCubeSkinnedParams);
            _lightComputeShader.BindUniformLocations(_computeLightParams);
        }

        public RenderTarget Render(Stage stage, Camera camera)
        {
            if (!_initialized)
            {
                InitializeHandles();
                _initialized = true;
            }

            RenderedLights = 0;

            // Init common matrices
            camera.GetViewMatrix(out var view);
            camera.GetProjectionMatrix(out var projection);

            // Render scene to GBuffer
            var clearColor = stage.ClearColor;
            clearColor.W = 0;
            _backend.ProfileBeginSection(Profiler.GBuffer);
            _backend.BeginPass(GBuffer, clearColor, ClearFlags.All);
            RenderScene(stage, camera, ref view, ref projection);
            _backend.EndPass();
            _backend.ProfileEndSection(Profiler.GBuffer);

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
                new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle },
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
            // TODO: Fix the actual geometry instead ... or maybe find a way to put the shadow maps into the tiled renderer
            var radius = light.Range * 10f;

            // Culling
            if (light.Type == LighType.PointLight)
            {
                if (!light.CastShadows)
                    return; // Handled by tiled renderer

                var boundingSphere = new BoundingSphere
                {
                    Center = light.Position,
                    Radius = radius
                };

                if (!cameraFrustum.Intersects(boundingSphere))
                    return;
            }

            RenderedLights++;

            var renderStateId = _directionalRenderState;

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
                renderStateId = _lightOutsideRenderState;
                if (cameraDistanceToLight.Length <= radius)
                {
                    renderStateId = _lightInsideRenderState;
                }
            }
            else if (light.Type == LighType.SpotLight)
            {
                renderStateId = _lightOutsideRenderState;
                if (IsInsideSpotLight(light, camera))
                {
                    renderStateId = _lightInsideRenderState;
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
                    RenderShadowsCube(_pointShadowsRenderTarget, light, stage, camera, out shadowViewProjection, out shadowCameraClipPlane);
                }
                else if (light.Type == LighType.SpotLight)
                {
                    RenderShadows(_spotShadowsRenderTarget, light, stage, camera, 0, out shadowViewProjection, out shadowCameraClipPlane);
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
                    RenderShadows(_directionalShadowsRenderTarget[0], light, stage, camera, 0, out shadowViewProjections[0], out shadowCameraClipPlane);

                    // Cascade 2
                    camera.NearClipDistance = clipDistances.X;
                    camera.FarClipDistance = clipDistances.Z;
                    RenderShadows(_directionalShadowsRenderTarget[1], light, stage, camera, 1, out shadowViewProjections[1], out shadowCameraClipPlane);

                    // Cascade 3
                    camera.NearClipDistance = clipDistances.X;
                    camera.FarClipDistance = clipDistances.W;
                    RenderShadows(_directionalShadowsRenderTarget[2], light, stage, camera, 2, out shadowViewProjections[2], out shadowCameraClipPlane);

                    // Restore clip plane
                    camera.NearClipDistance = cameraNear;
                    camera.FarClipDistance = cameraFar;
                }
                _backend.ChangeRenderTarget(_lightAccumulationTarget);
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
                lightTypeOffset = _pointLightShaderOffset;
            else if (light.Type == LighType.SpotLight)
                lightTypeOffset = _spotLightShaderOffset;

            if (castShadows)
                lightTypeOffset += 1 + (int)Settings.ShadowQuality;

            var shader = _lightShaders[lightTypeOffset];
            var shaderParams = _lightParams[lightTypeOffset];

            // Setup textures and begin rendering with the chosen shader
            int[] textures;
            int[] samplers;
            if (castShadows)
            {
                if (light.Type == LighType.Directional)
                {
                    textures = new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, _directionalShadowsRenderTarget[0].Textures[0].Handle, _directionalShadowsRenderTarget[1].Textures[0].Handle, _directionalShadowsRenderTarget[2].Textures[0].Handle };
                    samplers = new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _shadowSampler, _shadowSampler, _shadowSampler };
                }
                else
                {
                    var shadowMapHandle = light.Type == LighType.PointLight ?
                    _pointShadowsRenderTarget.Textures[0].Handle : _spotShadowsRenderTarget.Textures[0].Handle;

                    textures = new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle, shadowMapHandle };
                    samplers = new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _shadowSampler };
                }
            }
            else
            {
                textures = new int[] { GBuffer.Textures[0].Handle, GBuffer.Textures[1].Handle, GBuffer.Textures[2].Handle, GBuffer.Textures[3].Handle };
                samplers = new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering };
            }

            _backend.BeginInstance(shader.Handle, textures, samplers, renderStateId);

            // Setup texture samplers
            _backend.BindShaderVariable(shaderParams.SamplerGBuffer0, 0);
            _backend.BindShaderVariable(shaderParams.SamplerGBuffer1, 1);
            _backend.BindShaderVariable(shaderParams.SamplerGBuffer2, 2);
            _backend.BindShaderVariable(shaderParams.samplerDepth, 3);

            // Common uniforms
            _backend.BindShaderVariable(shaderParams.ScreenSize, ref _screenSize);
            _backend.BindShaderVariable(shaderParams.ModelViewProjection, ref modelViewProjection);
            _backend.BindShaderVariable(shaderParams.LightColor, ref lightColor);
            _backend.BindShaderVariable(shaderParams.CameraPosition, ref camera.Position);

            if (light.Type == LighType.Directional || light.Type == LighType.SpotLight)
            {
                var lightDirWS = light.Direction.Normalize();

                _backend.BindShaderVariable(shaderParams.LightDirection, ref lightDirWS);
            }

            if (light.Type == LighType.PointLight || light.Type == LighType.SpotLight)
            {
                _backend.BindShaderVariable(shaderParams.LightPosition, ref light.Position);
                _backend.BindShaderVariable(shaderParams.LightRange, light.Range);
                _backend.BindShaderVariable(shaderParams.LightInvSquareRadius, 1.0f / (light.Range * light.Range));
            }

            if (light.Type == LighType.SpotLight)
            {
                var spotParams = new Vector2((float)System.Math.Cos(light.InnerAngle / 2.0f), (float)System.Math.Cos(light.OuterAngle / 2.0f));
                _backend.BindShaderVariable(shaderParams.SpotParams, ref spotParams);
            }

            var inverseViewProjectionMatrix = Matrix4.Invert(view * projection);
            _backend.BindShaderVariable(shaderParams.InvViewProjection, ref inverseViewProjectionMatrix);

            if (castShadows)
            {
                _backend.BindShaderVariable(shaderParams.ClipPlane, ref shadowCameraClipPlane);
                _backend.BindShaderVariable(shaderParams.ShadowBias, light.ShadowBias);

                var texelSize = 1.0f / (light.Type == LighType.Directional ? _directionalShadowsRenderTarget[0].Width : _spotShadowsRenderTarget.Width);
                _backend.BindShaderVariable(shaderParams.TexelSize, texelSize);

                if (light.Type == LighType.PointLight)
                {
                    _backend.BindShaderVariable(shaderParams.SamplerShadowCube, 4);
                }
                else if (light.Type == LighType.Directional)
                {
                    _backend.BindShaderVariable(shaderParams.SamplerShadow1, 4);
                    _backend.BindShaderVariable(shaderParams.SamplerShadow2, 5);
                    _backend.BindShaderVariable(shaderParams.SamplerShadow3, 6);

                    _backend.BindShaderVariable(shaderParams.ShadowViewProj1, ref shadowViewProjections[0]);
                    _backend.BindShaderVariable(shaderParams.ShadowViewProj2, ref shadowViewProjections[1]);
                    _backend.BindShaderVariable(shaderParams.ShadowViewProj3, ref shadowViewProjections[2]);
                    _backend.BindShaderVariable(shaderParams.ClipDistances, ref clipDistances);
                }
                else
                {
                    _backend.BindShaderVariable(shaderParams.SamplerShadow, 4);
                    _backend.BindShaderVariable(shaderParams.ShadowViewProj, ref shadowViewProjection);
                }
            }

            if (light.Type == LighType.Directional)
            {
                _backend.DrawMesh(_quadMesh.MeshHandle);
            }
            else if (light.Type == LighType.PointLight)
            {
                _backend.DrawMesh(_unitSphere.SubMeshes[0].Handle);
            }
            else
            {
                _backend.DrawMesh(_unitCone.SubMeshes[0].Handle);
            }

            _backend.EndInstance();
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
                _backend.UpdateBufferInline(_pointLightDataCSBuffer, index * sizeof(PointLightDataCS), (byte*)data);
            }
            _backend.BindBufferBase(0, _pointLightDataCSBuffer);

            var lightCount = index;
            var lightTileSize = 16;

            _backend.BeginInstance(_lightComputeShader.Handle, new int[] { GBuffer.Textures[3].Handle, _lightAccumulationTarget.Textures[0].Handle }, new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering }, _lightAccumulatinRenderState);

            var numTilesX = (uint)DispatchSize(lightTileSize, GBuffer.Textures[0].Width);
            var numTilesY = (uint)DispatchSize(lightTileSize, GBuffer.Textures[0].Height);

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
            _backend.BindImageTexture(0, GBuffer.Textures[0].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadOnly, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba8);
            _backend.BindImageTexture(1, GBuffer.Textures[1].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadOnly, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba16f);
            _backend.BindImageTexture(2, GBuffer.Textures[2].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadOnly, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba8);
            _backend.BindImageTexture(3, _lightAccumulationTarget.Textures[0].Handle, OpenTK.Graphics.OpenGL.TextureAccess.ReadWrite, OpenTK.Graphics.OpenGL.SizedInternalFormat.Rgba16f);

            _backend.Barrier(OpenTK.Graphics.OpenGL.MemoryBarrierFlags.AllBarrierBits);
            _backend.DispatchCompute((int)numTilesX, (int)numTilesY, 1);
            _backend.Barrier(OpenTK.Graphics.OpenGL.MemoryBarrierFlags.AllBarrierBits);
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
            _backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 1), ClearFlags.All);

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

            _shadowRenderOperations.Reset();
            stage.PrepareRenderOperations(view, _shadowRenderOperations, true, false);

            RenderOperation[] operations;
            int count;
            _shadowRenderOperations.GetOperations(out operations, out count);

            for (var i = 0; i < count; i++)
            {
                var world = operations[i].WorldMatrix;

                modelViewProjection = world * view * projection;

                Resources.ShaderProgram program;
                RenderShadowsParams shadowParams;

                if (operations[i].Skeleton != null)
                {
                    program = _renderShadowsSkinnedShader;
                    shadowParams = _renderShadowsSkinnedParams;
                }
                else
                {
                    program = _renderShadowsShader;
                    shadowParams = _renderShadowsParams;
                }

                _backend.BeginInstance(program.Handle, null, null, _shadowsRenderState);
                _backend.BindShaderVariable(shadowParams.ModelViewProjection, ref modelViewProjection);
                _backend.BindShaderVariable(shadowParams.ClipPlane, ref clipPlane);

                if (light.Type == LighType.Directional)
                {
                    float shadowBias = 0.05f * (cascadeIndex + 1);
                    _backend.BindShaderVariable(shadowParams.ShadowBias, shadowBias);
                }

                if (operations[i].Skeleton != null)
                {
                    _backend.BindShaderVariable(shadowParams.Bones, ref operations[i].Skeleton.FinalBoneTransforms);
                }

                _backend.DrawMesh(operations[i].MeshHandle);

                _backend.EndInstance();
            }

            _backend.EndPass();
        }

        private void RenderShadowsCube(RenderTarget renderTarget, Light light, Stage stage, Camera camera, out Matrix4 viewProjection, out Vector2 clipPlane)
        {
            _backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 1), ClearFlags.All);

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

            _shadowRenderOperations.Reset();
            stage.PrepareRenderOperations(light.Position, light.Range * 2, _shadowRenderOperations, true);

            RenderOperation[] operations;
            int count;
            _shadowRenderOperations.GetOperations(out operations, out count);

            for (var i = 0; i < count; i++)
            {
                var world = operations[i].WorldMatrix;

                Resources.ShaderProgram program;
                RenderShadowsParams shadowParams;

                if (operations[i].Skeleton != null)
                {
                    program = _renderShadowsSkinnedCubeShader;
                    shadowParams = _renderShadowsCubeSkinnedParams;
                }
                else
                {
                    program = _renderShadowsCubeShader;
                    shadowParams = _renderShadowsCubeParams;
                }

                _backend.BeginInstance(program.Handle, null, null, _shadowsRenderState);
                _backend.BindShaderVariable(shadowParams.Model, ref world);
                _backend.BindShaderVariable(shadowParams.ClipPlane, ref clipPlane);
                _backend.BindShaderVariable(shadowParams.ViewProjectionMatrices, ref viewProjectionMatrices);
                _backend.BindShaderVariable(shadowParams.LightPosition, ref light.Position);

                if (operations[i].Skeleton != null)
                {
                    _backend.BindShaderVariable(shadowParams.Bones, ref operations[i].Skeleton.FinalBoneTransforms);
                }

                _backend.DrawMesh(operations[i].MeshHandle);

                _backend.EndInstance();
            }
            _backend.EndPass();
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
