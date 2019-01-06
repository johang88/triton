using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer.RenderTargets;
using Triton.Renderer;

namespace Triton.Graphics.Deferred
{
    public class ShadowBufferRenderer
    {
        private readonly Backend _backend;

        private Vector2 _screenSize;
        private readonly RenderTarget _renderTarget;
        private readonly BatchBuffer _quadMesh;
        private readonly int _shadowSampler;

        private Resources.ShaderProgram[] _renderShadowsCSMShader = new Resources.ShaderProgram[(int)ShadowQuality.High + 1];
        private RenderShadowsCSMParams _renderShadowsCSMParams = new RenderShadowsCSMParams();

        private bool _handlesInitialized = false;

        public bool DebugCascades = false;

        public ShadowBufferRenderer(Backend backend, Triton.Resources.ResourceManager resourceManager, int width, int height)
        {
            _backend = backend ?? throw new ArgumentNullException("backend");

            _screenSize = new Vector2(width, height);
            _renderTarget = _backend.CreateRenderTarget("shadow_buffer", new Definition(width, height, false, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba8, Renderer.PixelType.Float, 0)
            }));

            _quadMesh = _backend.CreateBatchBuffer();
            _quadMesh.Begin();
            _quadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
            _quadMesh.End();

            _shadowSampler = _backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
            {
                { SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
                { SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
                { SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
                { SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
            });

            // Load resources
            var shadowQualities = new string[] { "SHADOW_QUALITY_LOWEST", "SHADOW_QUALITY_LOW", "SHADOW_QUALITY_MEDIUM", "SHADOW_QUALITY_HIGH" };
            for (var i = 0; i < _renderShadowsCSMShader.Length; i++)
            {
                _renderShadowsCSMShader[i] = resourceManager.Load<Resources.ShaderProgram>("/shaders/deferred/csm", shadowQualities[i]);
            }
        }

        internal void Resize(int width, int height)
        {
            _screenSize = new Vector2(width, height);
            _backend.ResizeRenderTarget(_renderTarget, width, height);
        }

        public RenderTarget Render(Camera camera, RenderTarget gbuffer, List<RenderTarget> csmRenderTargets, Matrix4[] shadowViewProjections, float[] clipDistances, ShadowQuality quality)
        {
            if (!_handlesInitialized)
            {
                _renderShadowsCSMShader[0].BindUniformLocations(_renderShadowsCSMParams);
                _handlesInitialized = true;
            }

            Matrix4 view, projection;
            camera.GetViewMatrix(out view);
            camera.GetProjectionMatrix(out projection);
            var inverseViewProjectionMatrix = Matrix4.Invert(view * projection);

            _backend.BeginPass(_renderTarget, Vector4.One);

            // Setup textures and samplers
            var textures = new int[] { gbuffer.Textures[3].Handle,
                        csmRenderTargets[0].Textures[0].Handle, csmRenderTargets[1].Textures[0].Handle, csmRenderTargets[2].Textures[0].Handle,
                        csmRenderTargets[3].Textures[0].Handle, csmRenderTargets[4].Textures[0].Handle };

            var samplers = new int[textures.Length];
            for (var i = 0; i < samplers.Length; i++)
            {
                samplers[i] = i == 0 ? _backend.DefaultSamplerNoFiltering : _backend.DefaultSamplerNoFiltering;
            }

            // Render shadows
            _backend.BeginInstance(_renderShadowsCSMShader[(int)quality].Handle, textures, samplers);

            _backend.BindShaderVariable(_renderShadowsCSMParams.SamplerDepth, 0);

            var shadowSamplers = new int[] { 1, 2, 3, 4, 5 };
            _backend.BindShaderVariable(_renderShadowsCSMParams.SamplerShadowCsm, ref shadowSamplers);
            _backend.BindShaderVariable(_renderShadowsCSMParams.ShadowViewProjCsm, ref shadowViewProjections);
            _backend.BindShaderVariable(_renderShadowsCSMParams.ShadowClipDistances, ref clipDistances);

            var cameraClipPlane = new Vector2(camera.NearClipDistance, camera.FarClipDistance);
            _backend.BindShaderVariable(_renderShadowsCSMParams.CameraClipPlane, ref cameraClipPlane);

            _backend.BindShaderVariable(_renderShadowsCSMParams.ScreenSize, ref _screenSize);
            var texelSize = 1.0f / (float)csmRenderTargets[0].Width;
            _backend.BindShaderVariable(_renderShadowsCSMParams.TexelSize, texelSize);

            _backend.BindShaderVariable(_renderShadowsCSMParams.InvViewProjection, ref inverseViewProjectionMatrix);
            _backend.BindShaderVariable(_renderShadowsCSMParams.DebugCascades, DebugCascades ? 1 : 0);


            _backend.DrawMesh(_quadMesh.MeshHandle);

            _backend.EndPass();

            return _renderTarget;
        }

        private class RenderShadowsCSMParams
        {
            public int SamplerDepth = 0;
            public int ScreenSize = 0;
            public int SamplerShadowCsm = 0;
            public int ShadowViewProjCsm = 0;
            public int ShadowClipDistances = 0;
            public int TexelSize = 0;
            public int InvViewProjection = 0;
            public int CameraClipPlane = 0;
            public int DebugCascades = 0;
        }
    }
}
