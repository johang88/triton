using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;
using System.Threading;
using OpenTK.Graphics;
using OpenTK;
using Triton.Renderer;
using ImGuiNET;
using OpenTK.Input;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Triton.Game
{
    public abstract class Game : IDisposable
    {
        public Triton.Graphics.Backend GraphicsBackend { get; private set; }
        public Triton.Common.IO.FileSystem FileSystem { get; private set; }

        public Triton.Common.ResourceGroupManager ResourceGroupManager { get; private set; }
        public Triton.Common.ResourceManager CoreResources { get; private set; }
        public Triton.Common.ResourceManager GameResources { get; private set; }

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
        public Graphics.Post.PostEffectManager PostEffectManager { get; private set; }

        public Triton.Physics.World PhysicsWorld { get; private set; }

        public float PhysicsStepSize = 1.0f / 100.0f;

        public Audio.AudioSystem AudioSystem { get; private set; }

        public World.GameObjectManager GameWorld;

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

        public Game(string name, string logPath = "logs/")
        {
            _name = name;
            Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.Console());
            Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.File(string.Format("{0}/{1}.txt", logPath, name)));

            FileSystem = new Common.IO.FileSystem(MountFileSystem());
            ResourceGroupManager = new Common.ResourceGroupManager(FileSystem);

            CoreResources = ResourceGroupManager.Add("core");
            GameResources = ResourceGroupManager.Add("game");

            ResolutionScale = 1.0f; // This is default for obvious reasons
        }

        public virtual void Dispose()
        {
            AudioSystem.Dispose();
            CoreResources.Dispose();
            GameResources.Dispose();
            _window.Dispose();
        }

        public void Run()
        {
            Running = true;

            _updateThread = new Thread(UpdateLoop)
            {
                Name = "Update Thread"
            };
            _updateThread.Start();

            RenderLoop();
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

            using (GraphicsBackend = new Triton.Graphics.Backend(CoreResources, _window.Width, _window.Height, ctx))
            {
                Triton.Graphics.Resources.ResourceLoaders.Init(CoreResources, GraphicsBackend, FileSystem);
                Triton.Graphics.Resources.ResourceLoaders.Init(GameResources, GraphicsBackend, FileSystem);

                _rendererReady.Set();

                while (Running)
                {
                    _window.CursorVisible = CursorVisible;
                    _window.ProcessEvents();

                    if (!_window.Exists)
                        break;

                    CoreResources.TickResourceLoading(100);
                    GameResources.TickResourceLoading(10);

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

            Physics.Resources.ResourceLoaders.Init(CoreResources, FileSystem);
            Physics.Resources.ResourceLoaders.Init(GameResources, FileSystem);

            DeferredRenderer = new Graphics.Deferred.DeferredRenderer(CoreResources, GraphicsBackend, GraphicsBackend.Width, GraphicsBackend.Height);
            PostEffectManager = new Graphics.Post.PostEffectManager(FileSystem, CoreResources, GraphicsBackend, GraphicsBackend.Width, GraphicsBackend.Height);

            AudioSystem = new Audio.AudioSystem(FileSystem);
            PhysicsWorld = new Triton.Physics.World(GraphicsBackend, GameResources);

            Stage = new Graphics.Stage(GameResources);
            Camera = new Graphics.Camera(new Vector2(GraphicsBackend.Width, GraphicsBackend.Height));

            InputManager = new Input.InputManager(_window.Bounds);

            GameWorld = new World.GameObjectManager(Stage, InputManager, GameResources, PhysicsWorld, Camera);

            LoadCoreResources();

            // Wait until all initial resources have been loaded
            while (!CoreResources.AllResourcesLoaded())
            {
                Thread.Sleep(1);
            }

            Log.WriteLine("Core resources loaded");

            LoadResources();

            while (!GameResources.AllResourcesLoaded())
            {
                Thread.Sleep(1);
            }

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            var accumulator = 0.0f;

            while (Running)
            {
                _frameCount++;
                watch.Restart();

                ElapsedTime += _frameTime;

                if (_window.Focused)
                {
                    InputManager.Update();
                }

                accumulator += _frameTime;
                while (accumulator >= PhysicsStepSize)
                {
                    PhysicsWorld.Update(PhysicsStepSize);
                    accumulator -= PhysicsStepSize;
                }

                AudioSystem.Update();

                GameWorld.Update(_frameTime);

                Update(_frameTime);

                RenderScene(_frameTime, watch);
                _frameTime = (float)watch.Elapsed.TotalSeconds;

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Feed render commands to the graphics backend.
        /// Only override this method if you wish to customize the rendering pipeline.
        /// </summary>
        protected virtual void RenderScene(float deltaTime, System.Diagnostics.Stopwatch watch)
        {
            GraphicsBackend.BeginScene();

            var lightOutput = DeferredRenderer.Render(Stage, Camera);

            var postProcessedResult = PostEffectManager.Render(Camera, DeferredRenderer.GBuffer, lightOutput, deltaTime);

            GraphicsBackend.BeginPass(null, Vector4.Zero, ClearFlags.Color);

            SpriteRenderer.RenderQuad(postProcessedResult.Textures[0], Vector2.Zero, new Vector2(_window.Width, _window.Height));
            SpriteRenderer.Render(_window.Width, _window.Height);

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
            IO io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(_window.Width, _window.Height);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(1); /// TODO !!!!
            io.DeltaTime = deltaTime;

            UpdateImGuiInput(io);

            ImGui.NewFrame();
            RenderUI(deltaTime);
            ImGui.Render();

            _imGuiRenderer.SubmitDrawCommands();
        }

        void UpdateImGuiInput(IO io)
        {
            MouseState cursorState = Mouse.GetCursorState();
            MouseState mouseState = Mouse.GetState();

            if (_window.Bounds.Contains(cursorState.X, cursorState.Y))
            {
                Point windowPoint = _window.PointToClient(new Point(cursorState.X, cursorState.Y));
                io.MousePosition = new System.Numerics.Vector2(windowPoint.X / io.DisplayFramebufferScale.X, windowPoint.Y / io.DisplayFramebufferScale.Y);
            }
            else
            {
                io.MousePosition = new System.Numerics.Vector2(-1f, -1f);
            }

            io.MouseDown[0] = mouseState.LeftButton == ButtonState.Pressed;
            io.MouseDown[1] = mouseState.RightButton == ButtonState.Pressed;
            io.MouseDown[2] = mouseState.MiddleButton == ButtonState.Pressed;

            float newWheelPos = mouseState.WheelPrecise;
            float delta = newWheelPos - _wheelPosition;
            _wheelPosition = newWheelPos;
            io.MouseWheel = delta;
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

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 300), Condition.Always);
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), Condition.Always, System.Numerics.Vector2.Zero);
            ImGui.BeginWindow("Frame stats", WindowFlags.NoResize | WindowFlags.NoMove | WindowFlags.NoCollapse);

            var graphicsFrameTime = GraphicsBackend.FrameTime * 1000.0f;
            ImGui.Text($"Graphics frame time: {graphicsFrameTime:0.00}ms");
            var updateFrameTime = _frameTime * 1000.0f;
            ImGui.Text($"Update frame time: {updateFrameTime:0.00}ms");
            ImGui.Text($"Average FPS: {averageFPS:0}");
            ImGui.Text($"FPS: {fps:0}");
            ImGui.Text($"Light count: {DeferredRenderer.RenderedLights}");

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
                var name = Common.HashedStringTable.GetString(new Common.HashedString(section.Name));

                ImGui.Text($"\t{name} {section.ElapsedMs:0.00}ms");
            }

            ImGui.EndWindow();
        }

        private void UpdateImGuiKeyModifiers()
        {
            var io = ImGui.GetIO();

            io.AltPressed = InputManager.IsKeyDown(Input.Key.AltLeft);
            io.CtrlPressed = InputManager.IsKeyDown(Input.Key.ControlLeft);
            io.ShiftPressed = InputManager.IsKeyDown(Input.Key.ShiftLeft);
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
            DebugFont = CoreResources.Load<Triton.Graphics.Resources.BitmapFont>("/fonts/system_font");
            SpriteRenderer = GraphicsBackend.CreateSpriteBatch();

            InitImGui();
        }

        private unsafe void InitImGui()
        {
            ImGui.GetIO().FontAtlas.AddDefaultFont();
            SetImGuiKeyMaps();

            _window.KeyDown += Window_KeyDown;
            _window.KeyUp += Window_KeyUp;

            _imGuiRenderer = new ImGuiRenderer(GraphicsBackend, CoreResources);
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

        /// <summary>
        /// Update the game
        /// </summary>
        /// <param name="frameTime"></param>
        protected virtual void Update(float frameTime)
        {
        }

        private void SetImGuiKeyMaps()
        {
            IO io = ImGui.GetIO();
            io.KeyMap[GuiKey.Tab] = (int)Input.Key.Tab;
            io.KeyMap[GuiKey.LeftArrow] = (int)Input.Key.Left;
            io.KeyMap[GuiKey.RightArrow] = (int)Input.Key.Right;
            io.KeyMap[GuiKey.UpArrow] = (int)Input.Key.Up;
            io.KeyMap[GuiKey.DownArrow] = (int)Input.Key.Down;
            io.KeyMap[GuiKey.PageUp] = (int)Input.Key.PageUp;
            io.KeyMap[GuiKey.PageDown] = (int)Input.Key.PageDown;
            io.KeyMap[GuiKey.Home] = (int)Input.Key.Home;
            io.KeyMap[GuiKey.End] = (int)Input.Key.End;
            io.KeyMap[GuiKey.Delete] = (int)Input.Key.Delete;
            io.KeyMap[GuiKey.Backspace] = (int)Input.Key.BackSpace;
            io.KeyMap[GuiKey.Enter] = (int)Input.Key.Enter;
            io.KeyMap[GuiKey.Escape] = (int)Input.Key.Escape;
            io.KeyMap[GuiKey.A] = (int)Input.Key.A;
            io.KeyMap[GuiKey.C] = (int)Input.Key.C;
            io.KeyMap[GuiKey.V] = (int)Input.Key.V;
            io.KeyMap[GuiKey.X] = (int)Input.Key.X;
            io.KeyMap[GuiKey.Y] = (int)Input.Key.Y;
            io.KeyMap[GuiKey.Z] = (int)Input.Key.Z;
        }
    }
}
