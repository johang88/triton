using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Deferred
{
    public class ShadowRenderer
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
        private readonly RenderShadowsParams[] _renderShadowsParams = new RenderShadowsParams[3];
        private readonly RenderShadowsParams[] _renderShadowsSkinnedParams = new RenderShadowsParams[3];

        private readonly int _shadowsRenderState;

        private Resources.ShaderProgram[] _renderShadowsShaders = new Resources.ShaderProgram[3];
        private Resources.ShaderProgram[] _renderShadowsSkinnedShaders = new Resources.ShaderProgram[3];

        private bool _handlesInitialized = false;

        public float PSSMLambda { get; set; } = 0.95f;
        public float[] ShadowBiases { get; set; } = new float[] { 0.01f, 0.001f, 0.001f, 0.001f, 0.001f };
        private int _diffuseSampler;

        private Backend _backend;

        public ShadowRenderer(Backend backend, Triton.Resources.ResourceManager resourceManager, int cascadeCount = MaxCascadeCount, int resolution = DefaultResolution)
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

                light.ShadowBias = ShadowBiases[i];

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

        private void RenderCascade(RenderTarget renderTarget, Components.LightComponent light, Stage stage, Camera camera, out Matrix4 viewProjection)
        {
            _backend.BeginPass(renderTarget, new Vector4(0, 0, 0, 0), ClearFlags.All);

            Vector3 unitZ = Vector3.UnitZ;
            Vector3.Transform(ref unitZ, ref light.Owner.Orientation, out var lightDir);
            lightDir = -lightDir.Normalize();

            CalculateViewProjection(camera.GetFrustum(), lightDir, out var view, out var projection);
            viewProjection = view * projection;

            RenderShadowMap(light, stage, lightDir, view, projection);

            _backend.EndPass();
        }

        public void RenderShadowMap(Components.LightComponent light, Stage stage, Vector3 lightDir, Matrix4 view, Matrix4 projection)
        {
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
                        (operations[i].Skeleton != null || operations[index].Material.Id != operations[i].Material.Id ||
                         operations[index].MeshHandle != operations[i].MeshHandle))
                    {
                        break;
                    }
                    else
                    {
                        _worldMatrices[instanceCount] = operations[index].WorldMatrix;
                        instanceCount++;
                        i = index;
                    }
                }

                _backend.UpdateBufferInline(_instanceBuffers[_currentInstanceBuffer], instanceCount, _worldMatrices);

                Resources.ShaderProgram program;
                RenderShadowsParams shadowParams;

                var lightTypeIndex = (int)light.Type;

                if (operations[i].Skeleton != null)
                {
                    program = _renderShadowsSkinnedShaders[lightTypeIndex];
                    shadowParams = _renderShadowsSkinnedParams[lightTypeIndex];
                }
                else
                {
                    // TODO: Handle alpha cut off textures, you know for trees and stuff
                    program = _renderShadowsShaders[lightTypeIndex];
                    shadowParams = _renderShadowsParams[lightTypeIndex];
                }

                _backend.BeginInstance(program.Handle, textures, samplers, _shadowsRenderState);

                var lightDirWSAndBias = new Vector4(light.Type == LighType.PointLight? light.Owner.Position : lightDir, light.ShadowBias);

                _backend.BindShaderVariable(shadowParams.LightDirectionAndBias, ref lightDirWSAndBias);
                _backend.BindShaderVariable(shadowParams.View, ref view);
                _backend.BindShaderVariable(shadowParams.Projection, ref projection);

                if (operations[i].Skeleton != null)
                {
                    _backend.BindShaderVariable(shadowParams.Bones, ref operations[i].Skeleton.FinalBoneTransforms);
                }

                _backend.DrawMeshInstanced(operations[i].MeshHandle, instanceCount, _instanceBuffers[_currentInstanceBuffer]);

                _currentInstanceBuffer = (_currentInstanceBuffer + 1) % _instanceBuffers.Length;

                _backend.EndInstance();
            }
        }
    }
}
