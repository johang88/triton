using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;
using Triton.Renderer.Meshes;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Deferred
{
    public class ShadowRenderer
    {
        // Settings
        private const int MaxCascadeCount = 5;
        private const int DefaultResolution = 2048;

        // Cascades and cascade matrices
        private readonly List<RenderTarget> _renderTargets = new List<RenderTarget>();
        private Matrix4[] _shadowViewProjections;
        private int _resolution;

        // Render operations
        private readonly RenderOperations _shadowRenderOperations = new RenderOperations();

        // Shader configurations
        private readonly RenderShadowsParams[] _renderShadowsParams = new RenderShadowsParams[3];
        private readonly RenderShadowsParams[] _renderShadowsSkinnedParams = new RenderShadowsParams[3];

        private readonly int _shadowsRenderState;

        private Resources.ShaderProgram[] _renderShadowsShaders = new Resources.ShaderProgram[3];
        private Resources.ShaderProgram[] _renderShadowsSkinnedShaders = new Resources.ShaderProgram[3];

        private bool _handlesInitialized = false;

        public float MinCascadeDistance = 0.0f;
        public float MaxCascadeDistance = 0.5f;
        public PartitionMode PartitionMode = PartitionMode.PSSM;
        public float PSSMLambda = 0.8f;
        public readonly float[] SplitDistances = new float[5] { 0.05f, 0.15f, 0.5f, 0.75f, 1.0f };
        public readonly float[] SplitBiases = new float[5] { 0.015f, 0.0015f, 0.0015f, 0.0015f, 0.0015f };
        public bool StabilizeCascades = true;

        private Backend _backend;

        private readonly PerFrameData[] _perFrameData = new PerFrameData[1];
        private int _perFrameDataBuffer;

        private int[] _perObjectDataBuffers = new int[16];
        private int _currentPerObjectDataBuffer = 0;
        private Matrix4[] _worldMatrices = new Matrix4[4096];
        private DrawMeshMultiData[] _drawMeshMultiData = new DrawMeshMultiData[4096];
        
        public ShadowRenderer(Backend backend, Triton.Resources.ResourceManager resourceManager, int cascadeCount = MaxCascadeCount, int resolution = DefaultResolution)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));

            // Create cascade render targets
            SetCascadeCountAndResolution(cascadeCount, DefaultResolution);

            // Setup render states
            _shadowsRenderState = _backend.CreateRenderState(false, true, true, enableCullFace: false);

            var vertexFormat = new VertexFormat(new VertexFormatElement[]
            {
                new VertexFormatElement(VertexFormatSemantic.InstanceTransform0, VertexPointerType.Float, 4, 0, 1),
                new VertexFormatElement(VertexFormatSemantic.InstanceTransform1, VertexPointerType.Float, 4, sizeof(float) * 4, 1),
                new VertexFormatElement(VertexFormatSemantic.InstanceTransform2, VertexPointerType.Float, 4, sizeof(float) * 8, 1),
                new VertexFormatElement(VertexFormatSemantic.InstanceTransform3, VertexPointerType.Float, 4, sizeof(float) * 12, 1)
            });

            // Load shaders
            _renderShadowsShaders = new Resources.ShaderProgram[]
            {
                resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows", "POINT"),
                resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows", "SPOT"),
                resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows", "DIRECTIONAL")
            };
            _renderShadowsSkinnedShaders = new Resources.ShaderProgram[]
            {
                resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows", "SKINNED;POINT"),
                resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows", "SKINNED;SPOT"),
                resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows", "SKINNED;DIRECTIONAL")
            };

            _perFrameDataBuffer = _backend.RenderSystem.CreateBuffer(BufferTarget.UniformBuffer, true);
            _backend.RenderSystem.SetBufferData(_perFrameDataBuffer, _perFrameData, true, true);

            for (var i = 0; i < _perObjectDataBuffers.Length; i++)
            {
                _perObjectDataBuffers[i] = _backend.RenderSystem.CreateBuffer(BufferTarget.ShaderStorageBuffer, true);
                _backend.RenderSystem.SetBufferData(_perObjectDataBuffers[i], new Matrix4[0], true, true);
            }
        }

        public void SetCascadeCountAndResolution(int count, int resolution)
        {
            if (_renderTargets.Count == count)
                return;

            _resolution = resolution;

            if (_renderTargets.Count > count)
            {
                while (_renderTargets.Count > count)
                {
                    var renderTarget = _renderTargets[_renderTargets.Count - 1];
                    _backend.RenderSystem.DestroyRenderTarget(renderTarget.Handle);
                    _renderTargets.Remove(renderTarget);
                }
            }

            foreach (var renderTarget in _renderTargets)
            {
                if (renderTarget.Width != resolution)
                {
                    _backend.ResizeRenderTarget(renderTarget, resolution, resolution);
                }
            }

            if (_renderTargets.Count < count)
            {
                for (var i = _renderTargets.Count; i < count; i++)
                {
                    _renderTargets.Add(_backend.CreateRenderTarget("directional_shadows", new Definition(resolution, resolution, true, new List<Definition.Attachment>()
                    {
                        new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0)
                    })));
                }
            }

            // No fancy stuff here :)
            _shadowViewProjections = new Matrix4[count];
        }

        public List<RenderTarget> RenderCSM(RenderTarget gbuffer, Components.LightComponent light, Stage stage, Camera camera, out Matrix4[] viewProjections, out float[] clipDistances)
        {
            // Basic camera setup
            clipDistances = new float[_renderTargets.Count + 1];

            var minDistance = MinCascadeDistance;
            var maxDistance = MaxCascadeDistance;

            // TODO: Depth reduction read back

            float lambda = 1.0f;
            if (PartitionMode == PartitionMode.PSSM)
            {
                lambda = PSSMLambda;
            }

            var nearClip = camera.NearClipDistance;
            var farClip = camera.FarClipDistance;
            var clipRange = farClip - nearClip;

            var minZ = nearClip + minDistance * clipRange;
            var maxZ = nearClip + maxDistance * clipRange;

            var range = maxZ - minZ;
            var ratio = maxZ / minZ;

            clipDistances[0] = (minZ - nearClip) / clipRange;
            for (var i = 0; i < _renderTargets.Count; i++)
            {
                var p = (i + 1) / (float)_renderTargets.Count;
                var log = minZ * (float)System.Math.Pow(ratio, p);
                var uniform = minZ + range * p;
                var d = lambda * (log - uniform) + uniform;

                clipDistances[i + 1] = (d - nearClip) / clipRange;
            }

            // Render each cascade in turn
            for (var i = 0; i < _renderTargets.Count; i++)
            {
                var prevSplitDistance = clipDistances[i];
                var splitDistance = clipDistances[i + 1];

                light.ShadowBias = SplitBiases[i];
                RenderCascade(_renderTargets[i], light, stage, camera, prevSplitDistance, splitDistance, i, out _shadowViewProjections[i]);
            }

            viewProjections = _shadowViewProjections;

            // TODO: Depth reduction schedule

            return _renderTargets;
        }

        private void RenderCascade(RenderTarget renderTarget, Components.LightComponent light, Stage stage, Camera camera, float prevSplitDistance, float splitDistance, int cascadeIndex, out Matrix4 shadowViewProjection)
        {
            // Calculate light direction
            Vector3 unitZ = Vector3.UnitZ;
            Vector3.Transform(ref unitZ, ref light.Owner.Orientation, out var lightDir);
            lightDir = -lightDir.Normalize();

            // Shadow view & projection matrices
            Matrix4 view, projection;
            camera.GetViewMatrix(out view);
            camera.GetProjectionMatrix(out projection);

            var frustumCornersWS = camera.GetFrustum().GetCorners();

            // Get the corners of the current cascade slice of the view frustum
            for (var i = 0; i < 4; i++)
            {
                var cornerRay = frustumCornersWS[i + 4] - frustumCornersWS[i];
                var nearCornerRay = cornerRay * prevSplitDistance;
                var farCornerRay = cornerRay * splitDistance;
                frustumCornersWS[i + 4] = frustumCornersWS[i] + farCornerRay;
                frustumCornersWS[i] = frustumCornersWS[i] + nearCornerRay;
            }

            // Calculate the centroid of the view frustum slice
            var frustumCenter = Vector3.Zero;
            for (var i = 0; i < 8; i++)
                frustumCenter = frustumCornersWS[i] + frustumCenter;
            frustumCenter *= 1.0f / 8.0f;

            Vector3 minExtents, maxExtents, upDir;
            if (StabilizeCascades)
            {
                // This needs to be constant for it to be stable
                upDir = new Vector3(0.0f, 1.0f, 0.0f);

                // Calculate the radius of a bounding sphere surrounding the frustum corners
                float sphereRadius = 0.0f;
                for (var i = 0; i < 8; i++)
                {
                    float dist = (frustumCornersWS[i] - frustumCenter).Length;
                    sphereRadius = System.Math.Max(sphereRadius, dist);
                }

                sphereRadius = (float)System.Math.Ceiling(sphereRadius * 16.0f) / 16.0f;

                maxExtents = new Vector3(sphereRadius, sphereRadius, sphereRadius);
                minExtents = -maxExtents;
            }
            else
            {
                camera.GetRightVector(out upDir);

                var lightCameraPosition = frustumCenter;
                var lookAt = frustumCenter - lightDir;
                var lightView = Matrix4.LookAt(lightCameraPosition, lookAt, upDir);

                var mins = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var maxes = -mins;

                for (var i = 0; i < 8; i++)
                {
                    var corner = Vector3.Transform(frustumCornersWS[i], lightView);
                    mins = new Vector3(System.Math.Min(mins.X, corner.X), System.Math.Min(mins.Y, corner.Y), System.Math.Min(mins.Z, corner.Z));
                    maxes = new Vector3(System.Math.Max(mins.X, corner.X), System.Math.Max(mins.Y, corner.Y), System.Math.Max(mins.Z, corner.Z));
                }

                minExtents = mins;
                maxExtents = maxes;
            }

            var cascadeExtents = maxExtents - minExtents;
            var shadowCameraPos = frustumCenter + lightDir * -minExtents.Z;

            var shadowView = Matrix4.LookAt(shadowCameraPos, frustumCenter, upDir);
            var shadowProjection = Matrix4.CreateOrthographicOffCenter(minExtents.X, maxExtents.X, minExtents.Y, maxExtents.Y, 0.0f, cascadeExtents.Z);

            if (StabilizeCascades)
            {
                // Create the rounding matrix, by projecting the world-space origin and determining
                // the fractional offset in texel space
                var shadowOrigin = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                shadowOrigin = Vector4.Transform(shadowOrigin, shadowView);
                shadowOrigin = shadowOrigin * (float)renderTarget.Width / 2.0f;

                var roundedOrigin = new Vector4((float)System.Math.Round(shadowOrigin.X), (float)System.Math.Round(shadowOrigin.Y), (float)System.Math.Round(shadowOrigin.Z), (float)System.Math.Round(shadowOrigin.W));
                var roundOffset = roundedOrigin - shadowOrigin;
                roundOffset = roundOffset * 2.0f / (float)renderTarget.Width;
                roundOffset.Z = 0.0f;
                roundOffset.W = 0.0f;

                shadowProjection.Row3 = shadowProjection.Row3 + roundOffset;
            }

            shadowViewProjection = shadowView * shadowProjection;

            _backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 0), ClearFlags.All);
            RenderShadowMap(light, stage, lightDir, shadowView, shadowProjection);
            _backend.EndPass();
        }

        public unsafe void RenderShadowMap(Components.LightComponent light, Stage stage, Vector3 lightDir, Matrix4 view, Matrix4 projection)
        {
            var textures = new int[] { };
            var samplers = new int[] { };

            if (!_handlesInitialized)
            {
                for (var i = 0; i < _renderShadowsParams.Length; i++)
                {
                    _renderShadowsParams[i] = new RenderShadowsParams();
                    _renderShadowsSkinnedParams[i] = new RenderShadowsParams();

                    _renderShadowsShaders[i].BindUniformLocations(_renderShadowsParams[i]);
                    _renderShadowsSkinnedShaders[i].BindUniformLocations(_renderShadowsSkinnedParams[i]);
                }

                _handlesInitialized = true;
            }

            _shadowRenderOperations.Reset();
            stage.PrepareRenderOperations(view * projection, _shadowRenderOperations, true, light.Type != LighType.Directional);

            _shadowRenderOperations.GetOperations(ref view, out var operations, out var count);

            // Upload per shadow render data
            _perFrameData[0].LightDirWSAndBias = new Vector4(light.Type == LighType.PointLight ? light.Owner.Position : lightDir, light.ShadowBias);
            _perFrameData[0].View = view;
            _perFrameData[0].Projection = projection;
            _perFrameData[0].ViewProjection = view * projection;

            fixed (PerFrameData* data = _perFrameData)
            {
                _backend.UpdateBufferInline(_perFrameDataBuffer, sizeof(PerFrameData), (byte*)data);
            }
            _backend.BindBufferBase(0, _perFrameDataBuffer);

            // Prepare world matrices
            for (var i = 0; i < count; i++)
            {
                _worldMatrices[i] = operations[i].WorldMatrix;
            }

            // Upload world matrices!
            fixed (Matrix4* worldMatrices = _worldMatrices)
            {
                _backend.UpdateBufferInline(_perObjectDataBuffers[_currentPerObjectDataBuffer], sizeof(Matrix4) * count, (byte*)worldMatrices);
            }
            _backend.BindBufferBase(1, _perObjectDataBuffers[_currentPerObjectDataBuffer]);

            _currentPerObjectDataBuffer = ++_currentPerObjectDataBuffer % _perObjectDataBuffers.Length;

            var lightTypeIndex = (int)light.Type;

            // Render ordinary meshes first
            var program = _renderShadowsShaders[lightTypeIndex];
            var shadowParams = _renderShadowsParams[lightTypeIndex];

            _backend.BeginInstance(program.Handle, textures, samplers, _shadowsRenderState);

            var drawCount = 0;
            for (var i = 0; i < count; i++)
            {
                if (operations[i].Skeleton != null)
                    continue;

                _drawMeshMultiData[drawCount].BaseInstance = 0;
                _drawMeshMultiData[drawCount].MeshHandle = operations[i].MeshHandle;
                drawCount++;
            }

            if (drawCount > 0)
            {
                _backend.DrawMesh(_drawMeshMultiData, drawCount);
            }

            program = _renderShadowsSkinnedShaders[lightTypeIndex];
            shadowParams = _renderShadowsSkinnedParams[lightTypeIndex];

            // Render skeletal meshes
            _backend.BeginInstance(program.Handle, textures, samplers, _shadowsRenderState);
            for (var i = 0; i < count; i++)
            {
                if (operations[i].Skeleton == null)
                    continue;

                _backend.BindShaderVariable(shadowParams.World, ref operations[i].WorldMatrix);
                _backend.BindShaderVariable(shadowParams.Bones, ref operations[i].Skeleton.FinalBoneTransforms);
                _backend.DrawMesh(operations[i].MeshHandle);
            }
        }

        private struct PerFrameData
        {
            public Vector4 LightDirWSAndBias;
            public Matrix4 View;
            public Matrix4 Projection;
            public Matrix4 ViewProjection;
        }
    }

    public enum PartitionMode
    {
        Logarithmic,
        PSSM,
        Manual
    }
}
