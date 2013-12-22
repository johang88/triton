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

		const float MovementSpeed = 5.0f;
		const float MouseSensitivity = 0.0025f;

		private ManualResetEvent RendererReady = new ManualResetEvent(false);

		private Triton.Graphics.Backend Backend;
		private Triton.Common.IO.FileSystem FileSystem;
		private Triton.Common.ResourceManager ResourceManager;
		private Thread UpdateThread;
		private Triton.Physics.World PhysicsWorld;
		private bool Running;

		private Triton.Graphics.Stage Stage;
		private List<GameObject> GameObjects = new List<GameObject>();

		public Program()
		{
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.Console());
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.File("Logs/Test.txt"));

			FileSystem = new Triton.Common.IO.FileSystem();
			ResourceManager = new Triton.Common.ResourceManager();

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

		GameObject CreateGameObject(string filename, Vector3 size, Vector3 position)
		{
			var body = PhysicsWorld.CreateBoxBody(size.X, size.Y, size.Z, position, false);

			var mesh = Stage.AddMesh(filename);
			mesh.World = Matrix4.CreateTranslation(position);

			var gameObject = new GameObject(mesh, body);
			GameObjects.Add(gameObject);

			return gameObject;
		}

		void UpdateLoop()
		{
			WaitHandle.WaitAll(new WaitHandle[] { RendererReady });

			PhysicsWorld = new Triton.Physics.World(Backend, ResourceManager);
			var inputManager = new InputManager(Backend.WindowBounds);

			var spriteShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/sprite");

			var deferredRenderer = new Triton.Graphics.Deferred.DeferredRenderer(ResourceManager, Backend, Width, Height);
			var hdrRenderer = new Triton.Graphics.HDR.HDRRenderer(ResourceManager, Backend, Width, Height);

			Stage = new Triton.Graphics.Stage(ResourceManager);

			Stage.AddMesh("models/walls");
			Stage.AddMesh("models/floor");
			Stage.AddMesh("models/floor001");
			Stage.AddMesh("models/ceiling");
			Stage.AddMesh("models/door");
			Stage.AddMesh("models/door001");
			Stage.AddMesh("models/walls001");

			PhysicsWorld.CreateBoxBody(90.0f, 0.1f, 90.0f, new Vector3(0, -0.05f, 0), true);
			PhysicsWorld.CreateBoxBody(0.1f, 5.0f, 90.0f, new Vector3(-4f, 0, 0), true);
			PhysicsWorld.CreateBoxBody(0.1f, 5.0f, 90.0f, new Vector3(4f, 0, 0), true);

			CreateGameObject("models/crate", new Vector3(1, 1, 1), new Vector3(1.5f, 0.55f, 4));
			CreateGameObject("models/crate", new Vector3(1, 1, 1), new Vector3(0, 0.55f, 4));
			CreateGameObject("models/crate", new Vector3(1, 1, 1), new Vector3(-1.5f, 0.55f, 4));

			Stage.AmbientColor = new Vector3(0.1f, 0.1f, 0.1f);

			Stage.CreateSpotLight(new Vector3(0, 0.5f, -2), Vector3.UnitZ, 0.1f, 0.6f, 16.0f, new Vector3(1, 1, 1.2f) * 1.5f, true, 0.01f);

			var light = Stage.CreatePointLight(new Vector3(0, 1.5f, 0), 10.0f, new Vector3(1, 0.2f, 0.2f), true, 0.01f);

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
			var isFDown = false;

			var flashlight = Stage.CreateSpotLight(camera.Position, Vector3.UnitZ, 0.1f, 0.9f, 16.0f, new Vector3(1.2f, 1.1f, 0.8f), false, 0.05f);
			flashlight.Enabled = false;

			Backend.CursorVisible = false;

			var quad = Backend.CreateBatchBuffer();
			quad.Begin();
			quad.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			quad.End();

			hdrRenderer.WhitePoint = new Vector3(1, 1, 1) * 11.1f;

			var rng = new Random();
			var elapsedTime = 0.0f;

			bool debugPhysics = false;
			bool isBDown = false;

			var characterController = PhysicsWorld.CreateCharacterController(1.8f, 0.5f);
			characterController.SetPosition(new Vector3(0, 1.4f, 0));

			var texture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/misc_WoodenCrate_1k_d");
			var spriteBatch = Backend.CreateSpriteBatch();

			var accumulator = 0.0f;
			var physicsStepSize = 1.0f / 100.0f;

			while (Running)
			{
				var deltaTime = (float)stopWatch.Elapsed.TotalSeconds;
				elapsedTime += deltaTime;

				stopWatch.Restart();

				if (Backend.HasFocus)
					inputManager.Update();

				accumulator += deltaTime;
				while (accumulator >= physicsStepSize)
				{
					PhysicsWorld.Update(physicsStepSize);
					accumulator -= physicsStepSize;
				}

				foreach (var gameObject in GameObjects)
				{
					gameObject.Update();
				}

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

				if (movement.LengthSquared > 0.0f)
				{
					movement = movement.Normalize();
				}

				var movementDir = Quaternion.FromAxisAngle(Vector3.UnitY, cameraYaw);
				movement = Vector3.Transform(movement * MovementSpeed, movementDir);

				cameraYaw += -inputManager.MouseDelta.X * MouseSensitivity;
				cameraPitch += inputManager.MouseDelta.Y * MouseSensitivity;

				camera.Orientation = Quaternion.Identity;
				camera.Yaw(cameraYaw);
				camera.Pitch(cameraPitch);

				characterController.TryJump = inputManager.IsKeyDown(Key.Space);
				characterController.TargetVelocity = movement;
				camera.Position = characterController.Position;

				if (inputManager.IsKeyDown(Key.F))
				{
					isFDown = true;
				}
				else if (isFDown)
				{
					isFDown = false;
					flashlight.Enabled = !flashlight.Enabled;
				}

				if (inputManager.IsKeyDown(Key.B))
				{
					isBDown = true;
				}
				else if (isBDown)
				{
					isBDown = false;
					debugPhysics = !debugPhysics;
				}

				if (inputManager.IsKeyDown(Key.C))
				{
					isCDown = true;
				}
				else if (isCDown)
				{
					isCDown = false;

					var pointLight = Stage.CreatePointLight(camera.Position - new Vector3(0, 1.0f, 0), 4.0f + (float)rng.NextDouble() * 5.0f,
						new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble()), false);
					pointLight.Intensity = (0.3f + (float)rng.NextDouble() * 2.0f);
				}

				//light.Direction = Vector3.Transform(Vector3.UnitZ, Quaternion.FromAxisAngle(Vector3.UnitY, lightRotation));
				//lightRotation += deltaTime;

				light.Intensity = 1.5f + (float)System.Math.Sin(elapsedTime * 3.5f);

				flashlight.Position = camera.Position;
				flashlight.Position.Y -= 1.2f;
				flashlight.Direction = Vector3.Transform(new Vector3(0, 0, 1.0f), camera.Orientation);

				if (inputManager.IsKeyDown(Key.K))
					hdrRenderer.Exposure -= 1.0f * deltaTime;
				if (inputManager.IsKeyDown(Key.L))
					hdrRenderer.Exposure += 1.0f * deltaTime;

				Backend.BeginScene();
				var lighingOutput = deferredRenderer.Render(Stage, camera);
				hdrRenderer.Render(camera, lighingOutput);

				if (debugPhysics)
				{
					Backend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
					PhysicsWorld.DrawDebugInfo(camera);
					Backend.EndPass();
				}

				spriteBatch.RenderQuad(texture, Vector2.Zero, new Vector2(512, 512));
				spriteBatch.Render(1280, 720);

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
