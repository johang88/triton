using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Test
{
	class Program
	{
		ManualResetEvent RendererReady = new ManualResetEvent(false);

		void Run()
		{
			var workThread = new WorkerThread();

			var fileSystem = new Triton.Common.IO.FileSystem();
			var resourceManager = new Triton.Common.ResourceManager(workThread.AddItem);

			fileSystem.AddPackage("FileSystem", "../../../data");

			using (var backend = new Triton.Graphics.Backend(1280, 720, "Awesome Test Application", false, () => RendererReady.Set()))
			{
				WaitHandle.WaitAll(new WaitHandle[] { RendererReady });

				Triton.Graphics.Resources.ResourceLoaders.Init(resourceManager, backend, fileSystem);

				var shader = resourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/generic");
				var mesh = resourceManager.Load<Triton.Graphics.Resources.Mesh>("models/test");
				var texture = resourceManager.Load<Triton.Graphics.Resources.Texture>("textures/test");
			}
		}

		static void Main(string[] args)
		{
			var app = new Program();
			app.Run();
		}
	}
}
