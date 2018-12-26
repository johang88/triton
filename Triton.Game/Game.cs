using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using OpenTK.Graphics;
using OpenTK;
using Triton.Renderer;
using ImGuiNET;
using OpenTK.Input;
using System.Drawing;
using System.Runtime.InteropServices;
using Triton.Graphics;
using Triton.Logging;
using Triton.Utility;
using System.IO;

namespace Triton.Game
{
    public abstract class Game : IDisposable
    {
        public Triton.Graphics.Backend GraphicsBackend { get; private set; }
        public Triton.IO.FileSystem FileSystem { get; private set; }

        public Triton.Resources.ResourceGroupManager ResourceGroupManager { get; private set; }
        public Triton.Resources.ResourceManager Resources { get; private set; }

        private Thread _updateThread { get; set; }

        public bool Running { get; set; }

        private ManualResetEvent _rendererReady = new ManualResetEvent(false);

        public int RequestedWidth { get; set; }
        public int RequestedHeight { get; set; }
        public float ResolutionScale { get; set; }

        public Graphics.Stage Stage { get; private set; }
        public Graphics.Camera Camera { get; private set; }

        public Input.InputManager InputManager { get; private set; }

        public Graphics.Deferred.DeferredRenderer DeferredRenderer { get; private set; }
        public Graphics.Deferred.ShadowRenderer ShadowRenderer { get; private set; }
        public Graphics.Deferred.ShadowBufferRenderer ShadowBufferRenderer { get; private set; }
        public Graphics.Post.PostEffectManager PostEffectManager { get; private set; }

        public Triton.Physics.World PhysicsWorld { get; private set; }

        public Audio.AudioSystem AudioSystem { get; private set; }

        public GameObjectManager GameWorld;

        public Graphics.SpriteBatch SpriteRenderer;
        public Graphics.Resources.BitmapFont DebugFont;

        public DebugFlags DebugFlags;

        private long _frameCount = 0;
        private float _frameTime = 0.0f;
        protected float ElapsedTime = 0.0f;

        private OpenTK.INativeWindow _window;

        private readonly string _name;
        private float _wheelPosition;

        private ImGuiRenderer _imGuiRenderer;

        public bool CursorVisible { get; set; } = true;

        private readonly Concurrency.SingleThreadScheduler _mainThreadScheduler;
        private readonly Concurrency.SingleThreadScheduler _ioThreadScheduler;
        private readonly Thread _ioThread;

        private Services _services = new Services();

        public Game(string name, string logPath = "logs/")
        {
            _name = name;
            Log.AddOutputHandler(new Logging.Console());
            Log.AddOutputHandler(new Logging.File(string.Format("{0}/{1}.txt", logPath, name)));

            FileSystem = new IO.FileSystem(MountFileSystem());
            ResourceGroupManager = new Resources.ResourceGroupManager(FileSystem);

            Resources = ResourceGroupManager.Add("resources");

            ResolutionScale = 1.0f; // This is default for obvious reasons

            _ioThread = new Thread(IOThread) { Name = "IO Thread" };

            _mainThreadScheduler = new Concurrency.SingleThreadScheduler(System.Threading.Thread.CurrentThread);
            _ioThreadScheduler = new Concurrency.SingleThreadScheduler(_ioThread);
            Concurrency.TaskHelpers.Initialize(_mainThreadScheduler, _ioThreadScheduler);

            Running = true;
        }

        public virtual void Dispose()
        {
            GameWorld?.Clear();
            PhysicsWorld?.Dispose();
            AudioSystem.Dispose();
            Resources.Dispose();
            _window.Dispose();
        }

        public void Run()
        {
            Running = true;
            _ioThread.Start();

            _updateThread = new Thread(UpdateLoop)
            {
                Name = "Update Thread"
            };
            _updateThread.Start();

            RenderLoop();
        }

        private void IOThread()
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            while (Running)
            {
                _ioThreadScheduler.Tick(timer, 1000);
                Thread.Sleep(0);
            }
        }

        private void RenderLoop()
        {
            var graphicsMode = new GraphicsMode(new ColorFormat(32), 24, 0, 0);
            _window = new OpenTK.NativeWindow(RequestedWidth, RequestedHeight, _name, GameWindowFlags.Default, graphicsMode, DisplayDevice.Default)
            {
                Visible = true,
                CursorVisible = CursorVisible
            };

            var context = new GraphicsContext(graphicsMode, _window.WindowInfo, 4, 5, GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug);
            context.MakeCurrent(_window.WindowInfo);
            context.LoadAll();

            var ctx = new ContextReference
            {
                Context = context,
                SwapBuffers = context.SwapBuffers
            };

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            using (GraphicsBackend = new Triton.Graphics.Backend(Resources, _window.Width, _window.Height, ctx))
            {
                Triton.Graphics.Resources.ResourceSerializers.Init(Resources, GraphicsBackend, FileSystem, new Graphics.Resources.ShaderHotReloadConfig
                {
                    Path = @"..\Data\core_data\shaders\",
                    BasePath = @"/shaders/",
                    Enable = true
                });

                _rendererReady.Set();

                while (Running)
                {
                    _window.CursorVisible = CursorVisible;
                    _window.ProcessEvents();

                    if (!_window.Exists)
                        break;

                    _mainThreadScheduler.Tick(timer, 16);

                    if (!GraphicsBackend.Process())
                        break;

                    Thread.Sleep(0);
                }

                Running = false;
            }
        }

        void UpdateLoop()
        {
            WaitHandle.WaitAll(new WaitHandle[] { _rendererReady });

            Physics.Resources.ResourceLoaders.Init(Resources, FileSystem);

            ShadowRenderer = new Graphics.Deferred.ShadowRenderer(GraphicsBackend, Resources);
            DeferredRenderer = new Graphics.Deferred.DeferredRenderer(Resources, GraphicsBackend, ShadowRenderer, GraphicsBackend.Width, GraphicsBackend.Height);
            ShadowBufferRenderer = new Graphics.Deferred.ShadowBufferRenderer(GraphicsBackend, Resources, GraphicsBackend.Width, GraphicsBackend.Height);
            PostEffectManager = new Graphics.Post.PostEffectManager(FileSystem, Resources, GraphicsBackend, GraphicsBackend.Width, GraphicsBackend.Height);

            AudioSystem = new Audio.AudioSystem(FileSystem);
            PhysicsWorld = new Triton.Physics.World(GraphicsBackend, Resources);

            Stage = new Graphics.Stage();
            Camera = new Graphics.Camera(new Vector2(GraphicsBackend.Width, GraphicsBackend.Height));
            Stage.Camera = Camera;

            InputManager = new Input.InputManager(_window.Bounds);

            _services.Add(Stage);
            _services.Add(InputManager);
            _services.Add(Resources);
            _services.Add(PhysicsWorld);
            _services.Add(Camera);

            GameWorld = new GameObjectManager(_services);

            LoadCoreResources();

            // Wait until all initial resources have been loaded
            while (!Resources.AllResourcesLoaded())
            {
                Thread.Sleep(1);
            }

            Log.WriteLine("Core resources loaded");
            LoadResources();

            while (!Resources.AllResourcesLoaded())
            {
                Thread.Sleep(1);
            }

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            var stepSize = 1.0f / 60.0f;
            var accumulator = 0.0f;

            while (Running)
            {
                _frameCount++;

                _frameTime = (float)watch.Elapsed.TotalSeconds;
                watch.Restart();

                ElapsedTime += _frameTime;

                if (_window.Focused)
                {
                    InputManager.UiHasFocus = CursorVisible;
                    InputManager.Update();
                }
                else
                {
                    InputManager.UiHasFocus = true;
                }

                accumulator += _frameTime;
                while (accumulator >= stepSize)
                {
                    accumulator -= stepSize;
                    PhysicsWorld.Update(stepSize);
                }

                AudioSystem.Update();

                GameWorld.Update(_frameTime);

                Update(_frameTime);

                RenderScene(_frameTime, watch);

                Thread.Sleep(1);
            }

            UnloadResources();
        }

        /// <summary>
        /// Feed render commands to the graphics backend.
        /// Only override this method if you wish to customize the rendering pipeline.
        /// </summary>
        protected virtual void RenderScene(float deltaTime, System.Diagnostics.Stopwatch watch)
        {
            GraphicsBackend.BeginScene();

            var gbuffer = DeferredRenderer.RenderGBuffer(Stage, Camera);
            var sunLight = Stage.GetSunLight();

            // Prepare shadow buffer for sunlight
            List<RenderTarget> csm = null; RenderTarget shadows = null;
            if (sunLight != null)
            {
                GraphicsBackend.ProfileBeginSection(Profiler.ShadowsGeneration);
                csm = ShadowRenderer.RenderCSM(gbuffer, sunLight, Stage, Camera, out var viewProjections, out var clipDistances);
                GraphicsBackend.ProfileEndSection(Profiler.ShadowsGeneration);

                GraphicsBackend.ProfileBeginSection(Profiler.ShadowsRender);
                shadows = ShadowBufferRenderer.Render(Camera, gbuffer, csm, viewProjections, clipDistances, DeferredRenderer.Settings.ShadowQuality);
                GraphicsBackend.ProfileEndSection(Profiler.ShadowsRender);
            }

            // Light + post, ssao needed for ambient so we render it first
            GraphicsBackend.ProfileBeginSection(Profiler.SSAO);
            var ssao = PostEffectManager.RenderSSAO(Camera, gbuffer);
            GraphicsBackend.ProfileEndSection(Profiler.SSAO);
            var lightOutput = DeferredRenderer.RenderLighting(Stage, Camera, shadows);
            var postProcessedResult = PostEffectManager.Render(Camera, Stage, gbuffer, lightOutput, deltaTime);

            GraphicsBackend.BeginPass(null, Vector4.Zero, ClearFlags.Color);

            SpriteRenderer.RenderQuad(postProcessedResult.Textures[0], Vector2.Zero, new Vector2(_window.Width, _window.Height));
            SpriteRenderer.Render(_window.Width, _window.Height);

            if ((DebugFlags & DebugFlags.ShadowMaps) == DebugFlags.ShadowMaps)
            {
                var x = _window.Width - 10 - 256;
                var y = 10;

                SpriteRenderer.RenderQuad(DeferredRenderer.PointShadowAtlas.Textures[0], new Vector2(x - 256 - 10, y), new Vector2(256, 256));
                SpriteRenderer.RenderQuad(DeferredRenderer.SpotShadowAtlas.Textures[0], new Vector2(x, y), new Vector2(256, 256));

                

                if (csm != null)
                {
                    x = _window.Width;
                    y = RequestedHeight - 256 - 10;

                    var cascadeOffset = 256 + 10;
                    var xOffset = csm.Count * (cascadeOffset);

                    for (var i = 0; i < csm.Count; i++)
                    {
                        SpriteRenderer.RenderQuad(csm[i].Textures[0], new Vector2(x - xOffset + (i * cascadeOffset), y), new Vector2(256, 256));
                    }
                }

                SpriteRenderer.Render(_window.Width, _window.Height);
            }

            if ((DebugFlags & DebugFlags.Physics) == DebugFlags.Physics)
            {
                GraphicsBackend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), ClearFlags.Depth);
                PhysicsWorld.DrawDebugInfo(Camera);
                GraphicsBackend.EndPass();
            }

            DoRenderUI(deltaTime);
            SpriteRenderer.Render(_window.Width, _window.Height);

            GraphicsBackend.EndScene();
        }

        void DoRenderUI(float deltaTime)
        {
            var io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(_window.Width, _window.Height);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(1); /// TODO !!!!
            io.DeltaTime = deltaTime;

            UpdateImGuiInput(io);

            ImGui.NewFrame();
            RenderUI(deltaTime);
            ImGui.Render();

            _imGuiRenderer.SubmitDrawCommands();
        }

        void UpdateImGuiInput(ImGuiIOPtr io)
        {
            try
            {
                var cursorState = Mouse.GetCursorState();
                var mouseState = Mouse.GetState();

                if (_window.Bounds.Contains(cursorState.X, cursorState.Y))
                {
                    var windowPoint = _window.PointToClient(new Point(cursorState.X, cursorState.Y));
                    io.MousePos = new System.Numerics.Vector2(windowPoint.X / io.DisplayFramebufferScale.X, windowPoint.Y / io.DisplayFramebufferScale.Y);
                }
                else
                {
                    io.MousePos = new System.Numerics.Vector2(-1f, -1f);
                }

                io.MouseDown[0] = mouseState.LeftButton == ButtonState.Pressed;
                io.MouseDown[1] = mouseState.RightButton == ButtonState.Pressed;
                io.MouseDown[2] = mouseState.MiddleButton == ButtonState.Pressed;

                float newWheelPos = mouseState.WheelPrecise;
                float delta = newWheelPos - _wheelPosition;
                _wheelPosition = newWheelPos;
                io.MouseWheel = delta;
            }
            catch
            {
                // Awesome!!
            }
        }

        protected virtual void RenderUI(float deltaTime)
        {
            if ((DebugFlags & Triton.Game.DebugFlags.RenderStats) == Triton.Game.DebugFlags.RenderStats)
            {
                RenderFrameStats();
            }
        }

        private void RenderFrameStats()
        {
            ImGui.GetStyle().WindowRounding = 0;

            var averageFPS = _frameCount / ElapsedTime;
            var fps = 1.0f / _frameTime;

            var allocatedMemory = GC.GetTotalMemory(false) / 1024 / 1024;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 300));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10));
            ImGui.Begin("Frame stats", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

            var graphicsFrameTime = GraphicsBackend.FrameTime * 1000.0f;
            ImGui.Text($"Graphics frame time: {graphicsFrameTime:0.00}ms");
            var updateFrameTime = _frameTime * 1000.0f;
            ImGui.Text($"Update frame time: {updateFrameTime:0.00}ms");
            ImGui.Text($"Average FPS: {averageFPS:0}");
            ImGui.Text($"FPS: {fps:0}");
            ImGui.Text($"Light count: {DeferredRenderer.RenderedLights}");
            ImGui.Text($"Draw calls: {GraphicsBackend.DrawCalls}");

            ImGui.Separator();
            ImGui.Text($"GC Stats:");
            ImGui.Text($"\tAllocated memory: {allocatedMemory}");
            ImGui.Text($"\tCollection count:");
            for (var i = 0; i < GC.MaxGeneration; i++)
            {
                ImGui.Text($"\tGen ({i}): {GC.CollectionCount(i)}");
            }

            ImGui.Separator();
            ImGui.Text($"Profiler:");
            Graphics.Profiler.ProfilerSection[] sections;
            GraphicsBackend.SecondaryProfiler.GetSections(out sections, out var sectionCount);
            for (var i = 0; i < sectionCount; i++)
            {
                var section = sections[i];
                var name = HashedStringTable.GetString(new HashedString(section.Name));

                ImGui.Text($"\t{name} {section.ElapsedMs:0.00}ms");
            }

            ImGui.End();
        }

        private void UpdateImGuiKeyModifiers()
        {
            var io = ImGui.GetIO();

            io.KeyAlt = InputManager.IsKeyDown(Input.Key.AltLeft);
            io.KeyCtrl = InputManager.IsKeyDown(Input.Key.ControlLeft);
            io.KeyShift = InputManager.IsKeyDown(Input.Key.ShiftLeft);
        }

        /// <summary>
        /// Mount packages to the file system
        /// <see cref="Triton.Common.IO.FileSystem.AddPackage"/>
        /// </summary>
        protected abstract SharpFileSystem.IFileSystem MountFileSystem();

        /// <summary>
        /// Preload resources before the main loop is started
        /// </summary>
        protected virtual void LoadCoreResources()
        {
            DebugFont = Resources.Load<Triton.Graphics.Resources.BitmapFont>("/fonts/system_font");
            SpriteRenderer = GraphicsBackend.CreateSpriteBatch();

            InitImGui();
        }

        private unsafe void InitImGui()
        {
            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            SetImGuiKeyMaps();

            using (var stream = FileSystem.OpenRead("/fonts/Roboto-Regular.ttf"))
            {
                var data = new byte[stream.Length];

                long bytesRead = 0;
                while (bytesRead < stream.Length)
                {
                    bytesRead += stream.Read(data, (int)bytesRead, (int)(stream.Length - bytesRead));
                }

                fixed (byte* ptr = data)
                {

                    ImGui.GetIO().Fonts.AddFontFromMemoryTTF((IntPtr)ptr, data.Length, 16.0f);
                }
            }

            _window.KeyDown += Window_KeyDown;
            _window.KeyUp += Window_KeyUp;

            _imGuiRenderer = new ImGuiRenderer(GraphicsBackend, Resources);
        }

        private void Window_KeyUp(object sender, KeyboardKeyEventArgs e)
        {
            ImGui.GetIO().KeysDown[(int)e.Key] = true;
            UpdateImGuiKeyModifiers();
        }

        private void Window_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            ImGui.GetIO().KeysDown[(int)e.Key] = false;
            UpdateImGuiKeyModifiers();
        }

        /// <summary>
        /// Load resources
        /// </summary>
        protected virtual void LoadResources()
        {
        }

        protected virtual void UnloadResources()
        {
        }

        /// <summary>
        /// Update the game
        /// </summary>
        /// <param name="frameTime"></param>
        protected virtual void Update(float frameTime)
        {
            Resources.GargabgeCollect();
        }

        private void SetImGuiKeyMaps()
        {
            var io = ImGui.GetIO();

            io.KeyMap[(int)ImGuiKey.Tab] = (int)Input.Key.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Input.Key.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Input.Key.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Input.Key.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Input.Key.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Input.Key.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Input.Key.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Input.Key.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Input.Key.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Input.Key.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Input.Key.BackSpace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Input.Key.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Input.Key.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Input.Key.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Input.Key.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Input.Key.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Input.Key.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Input.Key.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Input.Key.Z;
        }
    }
}
