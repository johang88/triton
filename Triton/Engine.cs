using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
	public sealed class Engine : IDisposable
	{
		private readonly IWindowProvider WindowProvider;

		public readonly Common.IO.FileSystem FileSystem;
		public readonly Common.ResourceGroupManager ResourceGroupManager;
		private readonly Common.ResourceManager CoreResources;

		public readonly Triton.Graphics.Backend GraphicsBackend;
		public readonly RendererImplementation Renderer;

		public bool Running { get; set; }

		public Engine(IWindowProvider windowProvider, SharpFileSystem.IFileSystem fileSystem)
		{
			WindowProvider = windowProvider;

			// Initialize file system and resource manager
			FileSystem = new Common.IO.FileSystem(fileSystem);

			ResourceGroupManager = new Common.ResourceGroupManager(FileSystem);
			CoreResources = ResourceGroupManager.Add("core", 100);

			// Setup renderer
			GraphicsBackend = new Graphics.Backend(CoreResources, WindowProvider.Width, WindowProvider.Height, WindowProvider.WindowInfo);
			Renderer = new RendererImplementation(FileSystem, CoreResources, GraphicsBackend);
		}

		public void Dispose()
		{
			GraphicsBackend.Dispose();
		}

		// Start the update thread, TickMainThread will still have to be called from the main thread and a loop is required to keep everything alive
		public void Run()
		{
			var thread = new System.Threading.Thread(TickUpdateThread);
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
			ResourceGroupManager.TickResourceLoading();

			if (!GraphicsBackend.Process())
				return false;

			return true;
		}

		private void TickUpdateThread()
		{
		}
	}
}
