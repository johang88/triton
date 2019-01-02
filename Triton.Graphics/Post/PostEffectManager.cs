using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Post.Effects;
using Triton.Renderer.RenderTargets;
using Triton.Resources;

namespace Triton.Graphics.Post
{
    public class PostEffectManager
    {
        private readonly RenderTarget[] _temporaryRenderTargets = new RenderTarget[2];
        private readonly RenderTargetManager _renderTargetManager;
        private readonly Backend _backend;

        private readonly BatchBuffer _quadMesh;
        private SpriteBatch _sprite;

        // Settings
        public AntiAliasing AntiAliasing { get; set; }
        public AntiAliasingQuality AntiAliasingQuality { get; set; }
        public HDRSettings HDRSettings { get; set; }
        public DofSettings DofSettings { get; set; }
        private RenderTarget _ssaoOutput;

        public VisualizationMode VisualizationMode = Post.VisualizationMode.None;

        // Effects
        private readonly Effects.AdaptLuminance _adaptLuminance;
        private readonly Effects.Bloom _bloom;
        private readonly Effects.Tonemap _tonemap;
        private readonly Effects.Gamma _gamma;
        private readonly Effects.FXAA _fxaa;
        private readonly Effects.SMAA _smaa;
        private readonly Effects.Fog _fog;
        private readonly Effects.Visualize _visualize;
        private readonly Effects.SSAO _ssao;

        public bool EnablePostEffects = true;

        private List<BaseEffect> _effects = new List<BaseEffect>();

        public PostEffectManager(IO.FileSystem fileSystem, ResourceManager resourceManager, Backend backend, int width, int height)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            _backend = backend ?? throw new ArgumentNullException("backend");

            _renderTargetManager = new Post.RenderTargetManager(_backend);

            _temporaryRenderTargets[0] = _backend.CreateRenderTarget("post_0", new Definition(width, height, false, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0)
            }));

            _temporaryRenderTargets[1] = _backend.CreateRenderTarget("post_1", new Definition(width, height, false, new List<Definition.Attachment>()
            {
                new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0)
            }));

            _quadMesh = _backend.CreateBatchBuffer();
            _quadMesh.Begin();
            _quadMesh.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
            _quadMesh.End();

            // Setup effects
            _adaptLuminance = new Effects.AdaptLuminance(_backend, _quadMesh);
            _bloom = new Effects.Bloom(_backend, _quadMesh);
            _tonemap = new Effects.Tonemap(_backend, _quadMesh);
            _gamma = new Effects.Gamma(_backend, _quadMesh);
            _fxaa = new Effects.FXAA(_backend, _quadMesh);
            _smaa = new Effects.SMAA(_backend, fileSystem, _quadMesh);
            _fog = new Fog(_backend, _quadMesh);
            _visualize = new Visualize(_backend, _quadMesh);
            _ssao = new SSAO(_backend, _quadMesh);

            _effects.Add(_adaptLuminance);
            _effects.Add(_bloom);
            _effects.Add(_tonemap);
            _effects.Add(_gamma);
            _effects.Add(_fxaa);
            _effects.Add(_smaa);
            _effects.Add(_fog);
            _effects.Add(_visualize);
            _effects.Add(_ssao);

            // Default settings
            AntiAliasing = AntiAliasing.SMAA;
            AntiAliasingQuality = AntiAliasingQuality.Ultra;

            HDRSettings = new HDRSettings
            {
                AutoKey = true,
                KeyValue = 0.115f,
                AdaptationRate = 0.5f,
                BlurSigma = 3.0f,
                BloomThreshold = 9.0f,
                BloomStrength = 10.0f,
                EnableBloom = true,
                TonemapOperator = TonemapOperator.Uncharted
            };

            DofSettings = new DofSettings { Enable = false, CameraHeight = 720.0f, CocScale = 12.0f, FocusPlane = 1.0f };

            LoadResources(resourceManager);
        }

        void LoadResources(ResourceManager resourceManager)
        {
            _sprite = _backend.CreateSpriteBatch();

            foreach (var effect in _effects)
            {
                effect.LoadResources(resourceManager);
            }
        }

        void SwapRenderTargets()
        {
            var tmp = _temporaryRenderTargets[0];
            _temporaryRenderTargets[0] = _temporaryRenderTargets[1];
            _temporaryRenderTargets[1] = tmp;
        }

        private void ApplyAA()
        {
            switch (AntiAliasing)
            {
                case AntiAliasing.FXAA:
                    _fxaa.Render(_temporaryRenderTargets[0], _temporaryRenderTargets[1]);
                    SwapRenderTargets();
                    break;
                case AntiAliasing.SMAA:
                    _smaa.Render(AntiAliasingQuality, _temporaryRenderTargets[0], _temporaryRenderTargets[1]);
                    SwapRenderTargets();
                    break;
                case Post.AntiAliasing.Off:
                default:
                    break;
            }
        }

        private void ApplyLumianceBloomAndTonemap(float deltaTime)
        {
            RenderTarget bloom = null, lensFlares = null;

            var luminance = _adaptLuminance.Render(HDRSettings, _temporaryRenderTargets[0], deltaTime);

            if (HDRSettings.EnableBloom)
            {
                bloom = _bloom.Render(HDRSettings, _temporaryRenderTargets[0], luminance);
            }

            _tonemap.Render(HDRSettings, _temporaryRenderTargets[0], _temporaryRenderTargets[1], bloom, lensFlares, luminance);

            SwapRenderTargets();
        }

        private void ApplyFog(Camera camera, RenderTarget gbuffer, Stage stage)
        {
            _fog.Render(camera, stage, gbuffer, _temporaryRenderTargets[0], _temporaryRenderTargets[1]);
            SwapRenderTargets();
        }

        public RenderTarget RenderSSAO(Camera camera, RenderTarget gbuffer)
        {
            _ssaoOutput = _ssao.Render(camera, gbuffer);
            return _ssaoOutput;
        }

        public RenderTarget Render(Camera camera, Stage stage, RenderTarget gbuffer, RenderTarget input, float deltaTime)
        {
            _backend.ProfileBeginSection(Profiler.Post);

            // We always start by rendering the input texture to a temporary render target
            _backend.BeginPass(_temporaryRenderTargets[1], Vector4.Zero);

            _sprite.RenderQuad(input.Textures[0], Vector2.Zero);
            _sprite.Render(_temporaryRenderTargets[1].Width, _temporaryRenderTargets[1].Height);

            SwapRenderTargets();

            if (EnablePostEffects)
            {
                ApplyLumianceBloomAndTonemap(deltaTime);
                ApplyAA();
            }

            // linear -> to gamma space
            _gamma.Render(_temporaryRenderTargets[0], _temporaryRenderTargets[1]);
            SwapRenderTargets();

            if (VisualizationMode != VisualizationMode.None)
            {
                // Visualize does it's own gamma, but only if it wants to (some things are linear)
                _visualize.Render(VisualizationMode, camera, gbuffer, _ssaoOutput, _smaa, _temporaryRenderTargets[0], _temporaryRenderTargets[1]);
                SwapRenderTargets();
            }

            _backend.ProfileEndSection(Profiler.Post);
            return _temporaryRenderTargets[0];
        }

        public void Resize(int width, int height)
        {
            _backend.ResizeRenderTarget(_temporaryRenderTargets[0], width, height);
            _backend.ResizeRenderTarget(_temporaryRenderTargets[1], width, height);
            _bloom.Resize(width, height);
            _adaptLuminance.Resize(width, height);
            _gamma.Resize(width, height);
            _smaa.Resize(width, height);
            _fxaa.Resize(width, height);
            _tonemap.Resize(width, height);
        }
    }
}
