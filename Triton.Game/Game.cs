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

		public WorkerThread WorkerThread { get; private set; }
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
		public bool DebugPhysics = false;

		public Game(string name, string logPath = "logs/")
		{
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.Console());
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.File(string.Format("{0}/{1}.txt", logPath, name)));

			WorkerThread = new WorkerThread();

			FileSystem = new Triton.Common.IO.FileSystem();
			ResourceManager = new Triton.Common.ResourceManager(WorkerThread.AddItem);

			MountFileSystem();
		}

		public void Dispose()
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
				var frameTime = (float)watch.Elapsed.TotalSeconds;
				watch.Restart();

				if (GraphicsBackend.HasFocus)
				{
					InputManager.Update();
				}

				accumulator += frameTime;
				while (accumulator >= PhysicsStepSize)
				{
					PhysicsWorld.Update(PhysicsStepSize);
					accumulator -= PhysicsStepSize;
				}

				Update(frameTime);

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

			if (DebugPhysics)
			{
				GraphicsBackend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				PhysicsWorld.DrawDebugInfo(Camera);
				GraphicsBackend.EndPass();
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
