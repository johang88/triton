using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
	public sealed class Engine : IDisposable
	{
		private readonly Windowing.IWindowProvider WindowProvider;

		public readonly Common.IO.FileSystem FileSystem;
		public readonly Common.ResourceGroupManager ResourceGroupManager;

		private readonly Common.ResourceManager CoreResources;
		public readonly Common.ResourceManager GameResources;

		public readonly Triton.Graphics.Backend GraphicsBackend;
		public readonly RendererImplementation Renderer;

		public readonly Input.InputManager InputManager;

		public readonly GameWorld GameWorld;

		public readonly TaskList UpdateTasks = new TaskList();

		public bool Running { get; set; }

		public Engine(Windowing.IWindowProvider windowProvider, SharpFileSystem.IFileSystem fileSystem)
		{
			WindowProvider = windowProvider;

			// Initialize file system and resource manager
			FileSystem = new Common.IO.FileSystem(fileSystem);

			ResourceGroupManager = new Common.ResourceGroupManager(FileSystem);
			CoreResources = ResourceGroupManager.Add("core", 100);
			GameResources = ResourceGroupManager.Add("game", 10);

			// Setup graphics core
			GraphicsBackend = new Graphics.Backend(CoreResources, WindowProvider.Width, WindowProvider.Height, WindowProvider.WindowInfo);

			// Init resource loaders
			RegisterResourceLoaders(CoreResources);
			RegisterResourceLoaders(GameResources);

			// High level renderer (deferred + hdr)
			Renderer = new RendererImplementation(FileSystem, CoreResources, GraphicsBackend);

			// The input stuff
			InputManager = new Input.InputManager(WindowProvider.Bounds);

			// Setup game world
			GameWorld = new GameWorld(GameResources, GraphicsBackend);
		}

		public void Dispose()
		{
			GameResources.Dispose();
			CoreResources.Dispose();
			GraphicsBackend.Dispose();
		}

		public void RegisterResourceLoaders(Common.ResourceManager resourceManager)
		{
			Triton.Graphics.Resources.ResourceLoaders.Init(resourceManager, GraphicsBackend, FileSystem);
			Physics.Resources.ResourceLoaders.Init(resourceManager, FileSystem);
		}

		// Start the update thread, TickMainThread will still have to be called from the main thread and a loop is required to keep everything alive
		public void Run()
		{
			Running = true;

			// Start update thread
			var thread = new System.Threading.Thread(UpdateThread);
			thread.Name = "Update Thread";
			thread.Start();
		}

		/// <summary>
		/// Ticks the main thread, this will cause any queued render commands to be executed, pending resources will be loaded etc ...
		/// 
		/// MUST be called on the same thread that created the engine
		/// </summary>
		public bool TickMainThread()
		{
			if (!WindowProvider.Exists)
			{
				// Oh noes .. :/
				Running = false;
				return false;
			}

			ResourceGroupManager.TickResourceLoading();

			if (!GraphicsBackend.Process())
			{
				Running = false;
				return false;
			}

			return true;
		}

		private void UpdateThread()
		{
			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();

			while (Running)
			{
				var deltaTime = (float)watch.Elapsed.TotalSeconds;
				watch.Restart();

				InputManager.Update();

				// Run update tasks
				UpdateTasks.Execute(deltaTime);

				// Create render commands
				GameWorld.Camera.Viewport.X = WindowProvider.Width;
				GameWorld.Camera.Viewport.Y = WindowProvider.Height;

				Renderer.Render(GameWorld.GraphicsStage, GameWorld.Camera, deltaTime);
			}
		}
	}
}
