using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Triton;
using System.Collections.Concurrent;
using Triton.Input;

namespace Test
{
	class Program : IDisposable
	{
		const int Width = 1280;
		const int Height = 720;

		const float MovementSpeed = 6.0f;
		const float MouseSensitivity = 0.0025f;

		private ManualResetEvent RendererReady = new ManualResetEvent(false);

		private Triton.Graphics.Backend Backend;
		private Triton.Common.IO.FileSystem FileSystem;
		private Triton.Common.ResourceManager ResourceManager;
		private WorkerThread Worker = new WorkerThread();
		private Thread UpdateThread;
		private bool Running;

		public Program()
		{
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.Console());
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.File("Logs/Test.txt"));

			Worker = new WorkerThread();

			FileSystem = new Triton.Common.IO.FileSystem();
			ResourceManager = new Triton.Common.ResourceManager(Worker.AddItem);

			FileSystem.AddPackage("FileSystem", "../data");
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

			RenderLoop(); // The renderer runs on the main thread
		}

		void RenderLoop()
		{
			using (Backend = new Triton.Graphics.Backend(ResourceManager, Width, Height, "Awesome Test Application", false))
			{
				Triton.Graphics.Resources.ResourceLoaders.Init(ResourceManager, Backend, FileSystem);

				RendererReady.Set();

				while (Backend.Process() && Running)
				{
					Thread.Sleep(1);
				}

				Running = false;
			}
		}

		void UpdateLoop()
		{
			WaitHandle.WaitAll(new WaitHandle[] { RendererReady });

			var inputManager = new InputManager(Backend.WindowBounds);

			var deferredRenderer = new Triton.Graphics.Deferred.DeferredRenderer(ResourceManager, Backend, Width, Height);
			var stage = new Triton.Graphics.Stage(ResourceManager);

			var walls = stage.AddMesh("models/walls", "materials/wall");
			var floor = stage.AddMesh("models/floor", "materials/floor");
			var ceiling = stage.AddMesh("models/ceiling", "materials/ceiling");

			for (var i = 0; i < 4; i++)
			{
				var crate = stage.AddMesh("models/crate", "materials/crate");
				crate.Position = new Vector3(5.0f + i * 2.0f, 0.5f, 3.5f);
			}

			stage.AmbientColor = new Vector3(0.2f, 0.2f, 0.3f);

			while (!ResourceManager.AllResourcesLoaded())
			{
				Thread.Sleep(1);
			}

			var camera = new Triton.Graphics.Camera(new Vector2(Width, Height));
			camera.Position.X = 5.0f;
			camera.Position.Z = 3.0f;
			camera.Position.Y = 1.5f;
			float cameraYaw = 0.0f, cameraPitch = 0.0f;

			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();

			var isCDown = false;
			var isFDown = false;

			var flashlight = stage.CreateSpotLight(camera.Position, Vector3.UnitZ, 0.2f, 1.0f, 8.0f, new Vector3(1.2f, 1.1f, 0.8f) * 3.0f);
			flashlight.Enabled = false;

			while (Running)
			{
				var deltaTime = (float)stopWatch.Elapsed.TotalSeconds;
				stopWatch.Restart();

				if (Backend.HasFocus)
					inputManager.Update();

				if (inputManager.IsKeyDown(Key.Escape))
					Running = false;

				var movement = Vector3.Zero;
				if (inputManager.IsKeyDown(Key.W))
					movement.Z = 1.0f;
				else if (inputManager.IsKeyDown(Key.S))
					movement.Z = -1.0f;

				if (inputManager.IsKeyDown(Key.A))
					movement.X = 1.0f;
				else if (inputManager.IsKeyDown(Key.D))
					movement.X = -1.0f;

				cameraYaw += -inputManager.MouseDelta.X * MouseSensitivity;
				cameraPitch += inputManager.MouseDelta.Y * MouseSensitivity;

				camera.Orientation = Quaternion.Identity;
				camera.Yaw(cameraYaw);
				camera.Pitch(cameraPitch);

				var movementDir = Quaternion.FromAxisAngle(Vector3.UnitY, cameraYaw);

				movement = Vector3.Transform(movement * deltaTime * MovementSpeed, movementDir);
				camera.Move(ref movement);

				if (inputManager.IsKeyDown(Key.C))
				{
					isCDown = true;
				}
				else if (isCDown)
				{
					isCDown = false;

					stage.CreatePointLight(camera.Position, 5.0f, new Vector3(1.0f, 0.98f, 0.85f));
				}

				if (inputManager.IsKeyDown(Key.F))
				{
					isFDown = true;
				}
				else if (isFDown)
				{
					isFDown = false;

					flashlight.Enabled = !flashlight.Enabled;
				}

				flashlight.Position = camera.Position;
				flashlight.Direction = Vector3.Transform(new Vector3(0, -0.2f, 1.0f), camera.Orientation);

				Backend.BeginScene();
				deferredRenderer.Render(stage, camera);
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
