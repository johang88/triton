using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Triton;
using System.Collections.Concurrent;
using Triton.Input;
using System.Globalization;

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

			var spriteShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/sprite");

			var deferredRenderer = new Triton.Graphics.Deferred.DeferredRenderer(ResourceManager, Backend, Width, Height);
			var hdrRenderer = new Triton.Graphics.HDR.HDRRenderer(ResourceManager, Backend, Width, Height);

			var stage = new Triton.Graphics.Stage(ResourceManager);

			stage.AddMesh("models/walls");
			stage.AddMesh("models/floor");
			stage.AddMesh("models/floor001");
			stage.AddMesh("models/ceiling");
			stage.AddMesh("models/door");
			stage.AddMesh("models/door001");
			stage.AddMesh("models/walls001");

			stage.AddMesh("models/crate").Position = new Vector3(0, 0.0f, 2);

			stage.AmbientColor = new Vector3(0.1f, 0.1f, 0.1f);

			float lightZ = 5.0f;
			for (var i = 0; i < 10; i++)
			{
				//stage.CreatePointLight(new Vector3(0, 2.0f, lightZ), 7.0f, new Vector3(0.9f, 1.01f, 1.12f) * 0.6f);
				lightZ -= 5.0f;
			}

			stage.CreateSpotLight(new Vector3(0, 0.5f, -2), Vector3.UnitZ, 0.1f, 0.6f, 16.0f, new Vector3(1, 1, 1.2f), true, 0.01f);

			//stage.CreateDirectionalLight(new Vector3(0.3f, -0.4f, 0.3f), new Vector3(1, 1, 1.1f) * 2.4f);

			while (!ResourceManager.AllResourcesLoaded())
			{
				Thread.Sleep(1);
			}

			var spriteHandles = new SpriteHandles();
			spriteHandles.HandleDiffuse = spriteShader.GetAliasedUniform("DiffuseTexture");

			var camera = new Triton.Graphics.Camera(new Vector2(Width, Height));
			camera.Position.X = 0.0f;
			camera.Position.Z = 0.0f;
			camera.Position.Y = 1.5f;
			float cameraYaw = 0.0f, cameraPitch = 0.0f;

			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();

			var isCDown = false;

			Backend.CursorVisible = false;

			var quad = Backend.CreateBatchBuffer();
			quad.Begin();
			quad.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			quad.End();

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

					stage.CreatePointLight(camera.Position - new Vector3(0, 1.0f, 0), 10.0f, new Vector3(0.9f, 1.01f, 1.12f));
				}

				if (inputManager.IsKeyDown(Key.K))
					hdrRenderer.Exposure -= 1.0f * deltaTime;
				if (inputManager.IsKeyDown(Key.L))
					hdrRenderer.Exposure += 1.0f * deltaTime;

				Backend.BeginScene();
				var lighingOutput = deferredRenderer.Render(stage, camera);
				hdrRenderer.Render(camera, lighingOutput);

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

		class SpriteHandles
		{
			public int HandleDiffuse;
		}
	}
}
