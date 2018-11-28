using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Deferred
{
    public class CSMRenderer
    {
        // Settings
        private const int MaxCascadeCount = 5;
        private const int DefaultResolution = 2048;
        private const int NumInstances = 256;
        private const int NumInstanceBuffers = 16;

        // Cascades and cascade matrices
        private readonly List<RenderTarget> _renderTargets = new List<RenderTarget>();
        private Matrix4[] _shadowViewProjections;
        private int _resolution;

        // Render operations
        private readonly RenderOperations _shadowRenderOperations = new RenderOperations();
        private readonly Matrix4[] _worldMatrices = new Matrix4[NumInstances];
        private readonly int[] _instanceBuffers = new int[NumInstanceBuffers];
        private int _currentInstanceBuffer = 0;

        // Shader configurations
        private readonly RenderShadowsParams _renderShadowsParams = new RenderShadowsParams();
        private readonly RenderShadowsParams _renderShadowsSkinnedParams = new RenderShadowsParams();

        private readonly int _shadowsRenderState;

        private Resources.ShaderProgram _renderShadowsShader;
        private Resources.ShaderProgram _renderShadowsSkinnedShader;

        private bool _handlesInitialized = false;

        public float PSSMLambda { get; set; } = 0.95f;
        private int _diffuseSampler;

        private Backend _backend;

        public CSMRenderer(Backend backend, Triton.Resources.ResourceManager resourceManager, int cascadeCount = MaxCascadeCount, int resolution = DefaultResolution)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));

            // Create cascade render targets
            SetCascadeCountAndResolution(cascadeCount, DefaultResolution);

            // Setup render states
            _shadowsRenderState = _backend.CreateRenderState(false, true, true, enableCullFace: true);

            var vertexFormat = new VertexFormat(new VertexFormatElement[]
            {
                new VertexFormatElement(VertexFormatSemantic.InstanceTransform0, VertexPointerType.Float, 4, 0, 1),
                new VertexFormatElement(VertexFormatSemantic.InstanceTransform1, VertexPointerType.Float, 4, sizeof(float) * 4, 1),
                new VertexFormatElement(VertexFormatSemantic.InstanceTransform2, VertexPointerType.Float, 4, sizeof(float) * 8, 1),
                new VertexFormatElement(VertexFormatSemantic.InstanceTransform3, VertexPointerType.Float, 4, sizeof(float) * 12, 1)
            });

            for (var i = 0; i < _instanceBuffers.Length; i++)
            {
                _instanceBuffers[i] = _backend.RenderSystem.CreateBuffer(BufferTarget.ArrayBuffer, true, vertexFormat);
                _backend.RenderSystem.SetBufferData(_instanceBuffers[i], _worldMatrices, true, true);
            }

            _diffuseSampler = _backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
            {
                { SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapNearest },
                { SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest },
                { SamplerParameterName.TextureWrapS, (int)TextureWrapMode.Repeat },
                { SamplerParameterName.TextureWrapT, (int)TextureWrapMode.Repeat }
            });

            // Load shaders
            _renderShadowsShader = resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows");
            _renderShadowsSkinnedShader = resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/render_shadows", "SKINNED");
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
						//new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.R16f, Renderer.PixelType.Float, 0),
						new Definition.Attachment(Definition.AttachmentPoint.Depth, Renderer.PixelFormat.DepthComponent, Renderer.PixelInternalFormat.DepthComponent16, Renderer.PixelType.Float, 0)
                    })));
                }
            }

            // No fancy stuff here :)
            _shadowViewProjections = new Matrix4[count];
        }

        public List<RenderTarget> Render(RenderTarget gbuffer, Light light, Stage stage, Camera camera, out Matrix4[] viewProjections, out float[] clipDistances)
        {
            if (!_handlesInitialized)
            {
                _renderShadowsShader.BindUniformLocations(_renderShadowsParams);
                _renderShadowsSkinnedShader.BindUniformLocations(_renderShadowsSkinnedParams);

                _handlesInitialized = true;
            }

            // Basic camera setup
            clipDistances = new float[6];

            var cameraNear = camera.NearClipDistance;
            var cameraFar = camera.FarClipDistance;
            var clipDistanceNear = camera.NearClipDistance;
            var clipDistanceFar = camera.FarClipDistance;

            clipDistances[0] = clipDistanceNear;

            // Render each cascade in turn
            for (var i = 0; i < _renderTargets.Count; i++)
            {
                camera.NearClipDistance = clipDistanceNear;

                //if (i > 0) // Tight cascade fit
                //camera.NearClipDistance = clipDistances[i];

                camera.FarClipDistance = CalculateFarClipPlane(i, clipDistanceNear, clipDistanceFar);
                clipDistances[i + 1] = camera.FarClipDistance;

                RenderCascade(_renderTargets[i], light, stage, camera, out _shadowViewProjections[i]);
            }

            for (var i = 0; i < clipDistances.Length; i++)
            {
                clipDistances[i] = (clipDistances[i] - cameraNear) / (cameraFar - cameraNear);
            }

            // Restore clip planes
            camera.NearClipDistance = cameraNear;
            camera.FarClipDistance = cameraFar;

            viewProjections = _shadowViewProjections;

            return _renderTargets;
        }

        private float CalculateFarClipPlane(int i, float near, float far)
        {
            var fraction = (i + 1) / (float)_renderTargets.Count;
            var splitPoint = PSSMLambda * near * (float)System.Math.Pow(far / near, fraction) +
                                (1.0f - PSSMLambda) * (near + fraction * (far - near));

            return splitPoint;
        }

        private void CalculateViewProjection(BoundingFrustum cameraFrustum, Vector3 lightDir, out Matrix4 view, out Matrix4 projection)
        {
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
            var texelSize = 1.0f / (float)_renderTargets[0].Width;

            lightPosition = lightPosition / texelSize;
            lightPosition = new Vector3((float)System.Math.Floor(lightPosition.X), (float)System.Math.Floor(lightPosition.Y), (float)System.Math.Floor(lightPosition.Z)) * texelSize;

            view = Matrix4.LookAt(lightPosition, lightPosition - lightDir, Vector3.UnitY);
            projection = Matrix4.CreateOrthographic(boxSize.X, boxSize.Y, -boxSize.Z * 2.0f, boxSize.Z);
        }

        private void RenderCascade(RenderTarget renderTarget, Light light, Stage stage, Camera camera, out Matrix4 viewProjection)
        {
            _backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 0), ClearFlags.All);

            var lightDir = -light.Direction;
            lightDir.Normalize();

            CalculateViewProjection(camera.GetFrustum(), lightDir, out var view, out var projection);
            viewProjection = view * projection;

            _shadowRenderOperations.Reset();
            stage.PrepareRenderOperations(view, _shadowRenderOperations, true, false);

            int[] textures = new int[] { 0 };
            int[] samplers = new int[] { _diffuseSampler };

            _shadowRenderOperations.GetOperations(out var operations, out var count);

            for (var i = 0; i < count; i++)
            {
                var instanceCount = 0;

                for (var index = i; index < count; index++)
                {
                    if (instanceCount == NumInstances || index != i &&
                        (operations[index].Skeleton != null || operations[index].Material.Id != operations[i].Material.Id ||
                         operations[index].MeshHandle != operations[i].MeshHandle))
                    {
                        break;
                    }
                    else
                    {
                        Matrix4.Mult(ref operations[index].WorldMatrix, ref viewProjection, out _worldMatrices[instanceCount]);
                        instanceCount++;
                        i = index;
                    }
                }

                _backend.UpdateBufferInline(_instanceBuffers[_currentInstanceBuffer], instanceCount, _worldMatrices);

                Resources.ShaderProgram program;
                RenderShadowsParams shadowParams;

                if (operations[i].Skeleton != null)
                {
                    program = _renderShadowsSkinnedShader;
                    shadowParams = _renderShadowsSkinnedParams;
                }
                else
                {
                    // TODO: Handle alpha cut off textures, you know for trees and stuff
                    program = _renderShadowsShader;
                    shadowParams = _renderShadowsParams;
                }

                _backend.BeginInstance(program.Handle, textures, samplers, _shadowsRenderState);

                var lightDirWSAndBias = new Vector4(light.Direction.Normalize(), light.ShadowBias);
                _backend.BindShaderVariable(0, ref lightDirWSAndBias);

                if (operations[i].Skeleton != null)
                {
                    _backend.BindShaderVariable(shadowParams.Bones, ref operations[i].Skeleton.FinalBoneTransforms);
                }

                _backend.DrawMeshInstanced(operations[i].MeshHandle, instanceCount, _instanceBuffers[_currentInstanceBuffer]);

                _currentInstanceBuffer = (_currentInstanceBuffer + 1) % _instanceBuffers.Length;

                _backend.EndInstance();
            }

            _backend.EndPass();
        }
    }
}
