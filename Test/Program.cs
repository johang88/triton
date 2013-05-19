using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using OpenTK;

namespace Test
{
	class Program
	{
		ManualResetEvent RendererReady = new ManualResetEvent(false);
		ManualResetEvent RendererShuttingDown = new ManualResetEvent(false);
		ManualResetEvent MainLoopReady = new ManualResetEvent(true);

		void Run()
		{
			var workThread = new WorkerThread();

			var fileSystem = new Triton.Common.IO.FileSystem();
			var resourceManager = new Triton.Common.ResourceManager(workThread.AddItem);

			fileSystem.AddPackage("FileSystem", "../../../data");

			var backend = new Triton.Graphics.Backend(1280, 720, "Awesome Test Application", false, () => RendererReady.Set());
			backend.OnShuttingDown += () => RendererShuttingDown.Set();

			WaitHandle.WaitAll(new WaitHandle[] { RendererReady });

			Triton.Graphics.Resources.ResourceLoaders.Init(resourceManager, backend, fileSystem);

			var shader = resourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/generic");
			var mesh = resourceManager.Load<Triton.Graphics.Resources.Mesh>("models/box");
			var texture = resourceManager.Load<Triton.Graphics.Resources.Texture>("textures/test");

			while (!resourceManager.AllResourcesLoaded())
			{
				Thread.Sleep(1);
			}

			var world = Matrix4.CreateTranslation(0, 0, -2.0f);
			var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(70.0f), 1280.0f / 720.0f, 0.001f, 1000.0f);

			Matrix4 mvp;
			Matrix4.Mult(ref world, ref projection, out mvp);

			int mvpHandle = 0;
			int samplerHandle = 0;

			backend.AddCommandToWorkQueue(() => mvpHandle = shader.GetUniform("modelViewProjection"));
			backend.AddCommandToWorkQueue(() => samplerHandle = shader.GetUniform("samplerDiffuse"));

			while (WaitHandle.WaitAny(new WaitHandle[] { RendererShuttingDown, MainLoopReady }) == 1)
			{
				backend.BeginScene();
				backend.BeginPass(new OpenTK.Vector4(0.25f, 0.5f, 0.75f, 1.0f));
				backend.BeginInstance(shader.Handle, new int[] { texture.Handle });
				backend.BindShaderVariable(mvpHandle, ref mvp);
				backend.BindShaderVariable(samplerHandle, 0);
				foreach (var handle in mesh.Handles)
				{
					backend.DrawMesh(handle);
				}
				backend.EndPass();
				backend.EndScene();

				Thread.Sleep(1);
				MainLoopReady.Set();
			}
		}

		static void Main(string[] args)
		{
			var app = new Program();
			app.Run();
		}
	}
}
