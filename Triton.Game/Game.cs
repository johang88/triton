using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;
using System.Threading;

namespace Triton.Game
{
	public class Game : IDisposable
	{
		public Triton.Graphics.Backend GraphicsBackend { get; private set; }
		public Triton.Common.IO.FileSystem FileSystem { get; private set; }
		public Triton.Common.ResourceManager ResourceManager { get; private set; }

		private Thread UpdateThread { get; set; }

		public bool Running { get; set; }

		private ManualResetEvent RendererReady = new ManualResetEvent(false);

		public int Width { get; set; }
		public int Height { get; set; }

		public Graphics.Stage Stage { get; private set; }
		public Graphics.Camera Camera { get; private set; }

		public Input.InputManager InputManager { get; private set; }

		public Graphics.Deferred.DeferredRenderer DeferredRenderer { get; private set; }
		public Graphics.HDR.HDRRenderer HDRRenderer { get; private set; }

		public Triton.Physics.World PhysicsWorld { get; private set; }

		public float PhysicsStepSize = 1.0f / 100.0f;

		public World.GameWorld GameWorld;

		public Graphics.SpriteBatch DebugSprite;
		public Graphics.Resources.BitmapFont DebugFont;

		public DebugFlags DebugFlags;

		private long FrameCount = 0;
		private float FrameTime = 0.0f;
		private float ElapsedTime = 0.0f;

		public Game(string name, string logPath = "logs/")
		{
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.Console());
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.File(string.Format("{0}/{1}.txt", logPath, name)));

			FileSystem = new Triton.Common.IO.FileSystem();
			ResourceManager = new Triton.Common.ResourceManager();

			MountFileSystem();
		}

		public virtual void Dispose()
		{
			ResourceManager.Dispose();
		}

		public void Run()
		{
			Running = true;

			UpdateThread = new Thread(UpdateLoop);
			UpdateThread.Name = "Update Thread";
			UpdateThread.Start();

			RenderLoop();
		}

		private void RenderLoop()
		{
			using (GraphicsBackend = new Triton.Graphics.Backend(ResourceManager, Width, Height, "Awesome Test Application", false))
			{
				Triton.Graphics.Resources.ResourceLoaders.Init(ResourceManager, GraphicsBackend, FileSystem);

				RendererReady.Set();

				while (GraphicsBackend.Process() && Running)
				{
					Thread.Sleep(1);
				}

				Running = false;
			}
		}

		void UpdateLoop()
		{
			WaitHandle.WaitAll(new WaitHandle[] { RendererReady });

			PhysicsWorld = new Triton.Physics.World(GraphicsBackend, ResourceManager);

			DeferredRenderer = new Graphics.Deferred.DeferredRenderer(ResourceManager, GraphicsBackend, Width, Height);
			HDRRenderer = new Graphics.HDR.HDRRenderer(ResourceManager, GraphicsBackend, Width, Height);

			Stage = new Graphics.Stage(ResourceManager);
			Camera = new Graphics.Camera(new Vector2(Width, Height));

			InputManager = new Input.InputManager(GraphicsBackend.WindowBounds);

			GameWorld = new World.GameWorld(Stage, InputManager, ResourceManager, PhysicsWorld, Camera);

			LoadResources();

			// Wait until all initial resources have been loaded
			while (!ResourceManager.AllResourcesLoaded())
			{
				Thread.Sleep(1);
			}

			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();

			var accumulator = 0.0f;

			while (Running)
			{
				FrameCount++;
				FrameTime = (float)watch.Elapsed.TotalSeconds;
				watch.Restart();

				ElapsedTime += FrameTime;

				if (GraphicsBackend.HasFocus)
				{
					InputManager.Update();
				}

				accumulator += FrameTime;
				while (accumulator >= PhysicsStepSize)
				{
					PhysicsWorld.Update(PhysicsStepSize);
					accumulator -= PhysicsStepSize;
				}

				GameWorld.Update(FrameTime);

				Update(FrameTime);

				RenderScene();
				Thread.Sleep(1);
			}
		}

		/// <summary>
		/// Feed render commands to the graphics backend.
		/// Only override this method if you wish to customize the rendering pipeline.
		/// </summary>
		protected virtual void RenderScene()
		{
			GraphicsBackend.BeginScene();

			var lightOutput = DeferredRenderer.Render(Stage, Camera);
			HDRRenderer.Render(Camera, lightOutput);

			if ((DebugFlags & DebugFlags.Physics) == DebugFlags.Physics)
			{
				GraphicsBackend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				PhysicsWorld.DrawDebugInfo(Camera);
				GraphicsBackend.EndPass();
			}

			if ((DebugFlags & DebugFlags.GBuffer) == DebugFlags.GBuffer)
			{
				var textures = DeferredRenderer.GBuffer.Textures;
				for (var i = 0; i < textures.Length; i++)
				{
					DebugSprite.RenderQuad(textures[i], new Vector2(129 * i + 1, 1), new Vector2(128, 128), Vector2.Zero, Vector2.One, Vector4.One, false);
				}

				DebugSprite.Render(Width, Height);
			}

			if ((DebugFlags & Triton.Game.DebugFlags.RenderStats) == Triton.Game.DebugFlags.RenderStats)
			{
				var averageFPS = FrameCount / ElapsedTime;
				var fps = 1.0f / FrameTime;

				var allocatedMemory = GC.GetTotalMemory(false) / 1024 / 1024;

				var offsetY = 1;

				DebugFont.DrawText(DebugSprite, new Vector2(4, 4), Vector4.One, "Frame stats:");
				DebugFont.DrawText(DebugSprite, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tFrame time: {0:0.00}ms", FrameTime * 1000.0f);
				DebugFont.DrawText(DebugSprite, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tAverage FPS: {0:0}", averageFPS);
				DebugFont.DrawText(DebugSprite, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tFPS:{0:0}", fps);
				DebugFont.DrawText(DebugSprite, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "GC stats:");
				DebugFont.DrawText(DebugSprite, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tAllocated memory {0}mb", allocatedMemory);

				DebugFont.DrawText(DebugSprite, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tCollection count");
				for (var i = 0; i < GC.MaxGeneration; i++ )
				{
					DebugFont.DrawText(DebugSprite, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\t\tGen ({0})\t{1}", i, GC.CollectionCount(i));
				}

				DebugSprite.Render(Width, Height);
			}

			GraphicsBackend.EndScene();
		}

		/// <summary>
		/// Mount packages to the file system
		/// <see cref="Triton.Common.IO.FileSystem.AddPackage"/>
		/// </summary>
		protected virtual void MountFileSystem()
		{
		}

		/// <summary>
		/// Preload resources before the main loop is started
		/// </summary>
		protected virtual void LoadResources()
		{
			DebugFont = ResourceManager.Load<Triton.Graphics.Resources.BitmapFont>("fonts/system_font");
			DebugSprite = GraphicsBackend.CreateSpriteBatch();
		}

		/// <summary>
		/// Update the game
		/// </summary>
		/// <param name="frameTime"></param>
		protected virtual void Update(float frameTime)
		{
		}
	}
}
