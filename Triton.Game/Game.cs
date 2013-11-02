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

		private Graphics.Deferred.DeferredRenderer DeferredRenderer;
		private Graphics.HDR.HDRRenderer HDRRenderer;

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

			while (Running)
			{
				var frameTime = (float)watch.Elapsed.TotalSeconds;
				watch.Restart();

				Update(frameTime);

				GraphicsBackend.BeginScene();

				var lightOutput = DeferredRenderer.Render(Stage, Camera);
				HDRRenderer.Render(Camera, lightOutput);

				GraphicsBackend.EndScene();
				Thread.Sleep(1);
			}
		}

		protected virtual void MountFileSystem()
		{
		}

		protected virtual void LoadResources()
		{
		}

		protected virtual void Update(float frameTime)
		{
		}
	}
}
