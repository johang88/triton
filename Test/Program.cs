using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Triton;

namespace Test
{
	class Program : IDisposable
	{
		private ManualResetEvent RendererReady = new ManualResetEvent(false);

		private Triton.Graphics.Backend Backend;
		private Triton.Common.IO.FileSystem FileSystem;
		private Triton.Common.ResourceManager ResourceManager;
		private WorkerThread WorkerThread;
		private Thread UpdateThread;
		private bool Running;

		public Program()
		{
			WorkerThread = new WorkerThread();

			FileSystem = new Triton.Common.IO.FileSystem();
			ResourceManager = new Triton.Common.ResourceManager(WorkerThread.AddItem);

			FileSystem.AddPackage("FileSystem", "../data");
		}

		public void Dispose()
		{
			WorkerThread.Stop();
		}

		public void Run()
		{
			Running = true;

			UpdateThread = new Thread(UpdateLoop);
			UpdateThread.Name = "Update Thread";
			UpdateThread.Start();

			RenderLoop(); // The renderer runs on the main thread
		}

		void RenderLoop()
		{
			using (Backend = new Triton.Graphics.Backend(ResourceManager, 1280, 720, "Awesome Test Application", false))
			{
				Triton.Graphics.Resources.ResourceLoaders.Init(ResourceManager, Backend, FileSystem);

				RendererReady.Set();

				while (Backend.Process())
				{
					Thread.Sleep(1);
				}

				Running = false;
			}
		}

		void UpdateLoop()
		{
			WaitHandle.WaitAll(new WaitHandle[] { RendererReady });

			var shader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/generic");
			var mesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/box");
			var texture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/test");

			var renderTarget = Backend.CreateRenderTarget("test", 512, 512, Triton.Renderer.PixelInternalFormat.Rgb8, 1, true);

			while (!ResourceManager.AllResourcesLoaded() || !renderTarget.IsReady)
			{
				Thread.Sleep(1);
			}

			int mvpHandle = 0;
			int samplerHandle = 0;

			mvpHandle = shader.GetAliasedUniform("ModelViewProjection");
			samplerHandle = shader.GetAliasedUniform("DiffuseTexture");

			var angle = 0.0f;
			var cameraPos = new Vector3(0, 1.8f, 2);

			while (Running)
			{
				angle += 0.001f;
				var world = Matrix4.CreateRotationY(angle) * Matrix4.CreateTranslation(0, 0, 0.0f);
				var view = Matrix4.LookAt(cameraPos, Vector3.Zero, Vector3.UnitY);
				var projection = Matrix4.CreatePerspectiveFieldOfView(1.22173f, 1.0f, 0.001f, 1000.0f);

				Matrix4 mvp = world * view * projection;

				Backend.BeginScene();

				Backend.BeginPass(renderTarget, new Vector4(0.7f, 0.55f, 0.25f, 1.0f));
				Backend.BeginInstance(shader.Handle, new int[] { texture.Handle });
				Backend.BindShaderVariable(mvpHandle, ref mvp);
				Backend.BindShaderVariable(samplerHandle, 0);
				foreach (var handle in mesh.Handles)
				{
					Backend.DrawMesh(handle);
				}
				Backend.EndPass();

				projection = Matrix4.CreatePerspectiveFieldOfView(1.22173f, 1280.0f / 720.0f, 0.001f, 1000.0f);
				mvp = world * view * projection;

				Backend.BeginPass(null, new Vector4(0.25f, 0.5f, 0.75f, 1.0f));
				Backend.BeginInstance(shader.Handle, new int[] { renderTarget.Textures[0].Handle });
				Backend.BindShaderVariable(mvpHandle, ref mvp);
				Backend.BindShaderVariable(samplerHandle, 0);
				foreach (var handle in mesh.Handles)
				{
					Backend.DrawMesh(handle);
				}
				Backend.EndPass();

				Backend.EndScene();

				Thread.Sleep(1);
			}
		}

		static void Main(string[] args)
		{
			using (var app = new Program())
			{
				app.Run();
			}
		}
	}
}
