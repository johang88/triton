using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Triton;
using System.Collections.Concurrent;

namespace Test
{
	class Program : IDisposable
	{
		const int Width = 1280;
		const int Height = 720;
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
			var pointLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/point_light");
			var finalPassShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/final");

			var unitSphere = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/unit_sphere");

			var wallsMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/walls");
			var floorMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/floor");
			var ceilingMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/ceiling");

			var texture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/strike_trooper_d");
			var normalMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/strike_trooper_n");

			var wallDiffuse = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/wall_d");
			var wallNormalMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/wall_n");

			var floorDiffuse = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/floor_d");
			var floorNormalMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/floor_n");

			var lightDir = new Vector3(0.0f, 2.0f, 0.0f);
			var lightColor = new Vector3(1.6f, 1.12f, 1.15f) * 2.0f;
			var ambientColor = new Vector3(0.2f, 0.2f, 0.3f);

			var lightStartPos = lightDir;
			var lightEndPos = lightDir + new Vector3(0, 0, 9.0f);
			var lightAlpha = 0.0f;
			var lightAlphaDir = 1.0f;

			var fullSceneRenderTarget = Backend.CreateRenderTarget("full_scene", Width, Height, Triton.Renderer.PixelInternalFormat.Rgba32f, 3, true);
			var lightAccumulationRenderTarget = Backend.CreateRenderTarget("light_accumulation", Width, Height, Triton.Renderer.PixelInternalFormat.Rgba32f, 1, true);

			var batchBuffer = Backend.CreateBatchBuffer();

			batchBuffer.Begin();
			batchBuffer.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			batchBuffer.End();

			while (!ResourceManager.AllResourcesLoaded())
			{
				Thread.Sleep(1);
			}

			var genericParams2 = new GenericParams();
			genericParams2.HandleMVP = shader.GetAliasedUniform("ModelViewProjection");
			genericParams2.HandleWorld = shader.GetAliasedUniform("World");
			genericParams2.HandleDiffuseTexture = shader.GetAliasedUniform("DiffuseTexture");
			genericParams2.HandleNormalMap = shader.GetAliasedUniform("NormalMap");

			var pointLightParams = new PointLightParams();
			pointLightParams.HandleMVP = pointLightShader.GetAliasedUniform("ModelViewProjection"); ;
			pointLightParams.HandleNormal = pointLightShader.GetAliasedUniform("NormalTexture");
			pointLightParams.HandlePosition = pointLightShader.GetAliasedUniform("PositionTexture");
			pointLightParams.HandleLightPositon = pointLightShader.GetAliasedUniform("LightPosition");
			pointLightParams.HandleCameraPosition = pointLightShader.GetAliasedUniform("CameraPosition");
			pointLightParams.HandleLightColor = pointLightShader.GetAliasedUniform("LightColor");
			pointLightParams.HandleLightRange = pointLightShader.GetAliasedUniform("LightRange");
			pointLightParams.HandleScreenSize = pointLightShader.GetAliasedUniform("ScreenSize");

			var finalParams = new FinalPassParams();
			finalParams.HandleMVP = finalPassShader.GetAliasedUniform("ModelViewProjection"); ;
			finalParams.HandleDiffuse = finalPassShader.GetAliasedUniform("DiffuseTexture");
			finalParams.HandleLight = finalPassShader.GetAliasedUniform("LightTexture");

			var angle = 0.0f;
			var cameraPos = new Vector3(0, 1.8f, -2.5f);

			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();

			var screenSize = new Vector2((float)Width, (float)Height);

			var lights = new List<PointLight>();
			var rng = new Random();

			for (var i = 0; i < 10; i++)
			{
				var color = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
				var range = 2.0f;
				var offset = 2.0f;

				lights.Add(new PointLight(new Vector3(-0.5f, 1.5f, 1.0f + i * offset), color, range));
				lights.Add(new PointLight(new Vector3(0.5f, 1.5f, 1.0f + i * offset), color, range));
			}

			while (Running)
			{
				var deltaTime = (float)stopWatch.Elapsed.TotalSeconds;
				stopWatch.Restart();

				angle += 1.4f * deltaTime;

				lightAlpha += deltaTime * 0.4f * lightAlphaDir;
				if (lightAlpha >= 1.0f)
					lightAlphaDir = -1.0f;
				else if (lightAlpha <= 0.0f)
					lightAlphaDir = 1.0f;

				lightDir = Vector3.Lerp(lightStartPos, lightEndPos, lightAlpha);

				Backend.BeginScene();

				// Render main scene

				var view = Matrix4.LookAt(cameraPos, cameraPos + Vector3.UnitZ, Vector3.UnitY);
				var projection = Matrix4.CreatePerspectiveFieldOfView(1.22173f, Width / (float)Height, 0.001f, 1000.0f);

				Backend.BeginPass(fullSceneRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

				var world = Matrix4.CreateFromAxisAngle(Vector3.UnitX, (float)(Math.PI / 2.0)) * Matrix4.CreateTranslation(0, 5f, 12.0f);
				RenderCorridor(shader, wallsMesh, floorMesh, ceilingMesh, wallDiffuse, wallNormalMap, floorDiffuse, floorNormalMap, genericParams2, ref world, ref view, ref projection);

				world = Matrix4.CreateFromAxisAngle(Vector3.UnitX, (float)(Math.PI / 2.0)) * Matrix4.CreateTranslation(0, 5f, 22.0f);
				RenderCorridor(shader, wallsMesh, floorMesh, ceilingMesh, wallDiffuse, wallNormalMap, floorDiffuse, floorNormalMap, genericParams2, ref world, ref view, ref projection);

				world = Matrix4.CreateFromAxisAngle(Vector3.UnitX, (float)(Math.PI / 2.0)) * Matrix4.CreateTranslation(0, 5f, 44.0f);
				RenderCorridor(shader, wallsMesh, floorMesh, ceilingMesh, wallDiffuse, wallNormalMap, floorDiffuse, floorNormalMap, genericParams2, ref world, ref view, ref projection);

				Backend.EndInstance();

				Backend.EndPass();

				// Render lights
				Backend.BeginPass(lightAccumulationRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

				var mvp = Matrix4.Identity;

				foreach (var light in lights)
				{
					var radius = light.Range;

					var cullFaceMode = Triton.Renderer.CullFaceMode.Back;
					var delta = light.Position - cameraPos;
					if (Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z) <= radius * radius)
					{
						cullFaceMode = Triton.Renderer.CullFaceMode.Front;
					}

					world = Matrix4.Scale(radius) * Matrix4.CreateTranslation(light.Position);
					mvp = world * view * projection;

					Backend.BeginInstance(pointLightShader.Handle, new int[] { fullSceneRenderTarget.Textures[1].Handle, fullSceneRenderTarget.Textures[2].Handle }, true, true, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, cullFaceMode);
					Backend.BindShaderVariable(pointLightParams.HandleNormal, 0);
					Backend.BindShaderVariable(pointLightParams.HandlePosition, 1);
					Backend.BindShaderVariable(pointLightParams.HandleScreenSize, ref screenSize);

					Backend.BindShaderVariable(pointLightParams.HandleMVP, ref mvp);
					Backend.BindShaderVariable(pointLightParams.HandleLightPositon, ref light.Position);
					Backend.BindShaderVariable(pointLightParams.HandleLightColor, ref light.Color);
					Backend.BindShaderVariable(pointLightParams.HandleLightRange, light.Range);
					Backend.BindShaderVariable(pointLightParams.HandleCameraPosition, ref cameraPos);

					Backend.DrawMesh(unitSphere.Handles[0]);
				}

				Backend.EndPass();

				mvp = Matrix4.Identity;

				// Render final scene	
				Backend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				Backend.BeginInstance(finalPassShader.Handle, new int[] { fullSceneRenderTarget.Textures[0].Handle, lightAccumulationRenderTarget.Textures[0].Handle });
				Backend.BindShaderVariable(finalParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(finalParams.HandleDiffuse, 0);
				Backend.BindShaderVariable(finalParams.HandleLight, 1);

				Backend.DrawMesh(batchBuffer.Mesh.Handles[0]);
				Backend.EndPass();

				Backend.EndScene();

				Thread.Sleep(1);
			}
		}

		private void RenderCorridor(Triton.Graphics.Resources.ShaderProgram shader, Triton.Graphics.Resources.Mesh wallsMesh, Triton.Graphics.Resources.Mesh floorMesh, Triton.Graphics.Resources.Mesh ceilingMesh, Triton.Graphics.Resources.Texture wallDiffuse, Triton.Graphics.Resources.Texture wallNormalMap, Triton.Graphics.Resources.Texture floorDiffuse, Triton.Graphics.Resources.Texture floorNormalMap, GenericParams genericParams2, ref Matrix4 world, ref Matrix4 view, ref Matrix4 projection)
		{
			// Render corridor
			Matrix4 mvp = world * view * projection;
			Backend.BeginInstance(shader.Handle, new int[] { wallDiffuse.Handle, wallNormalMap.Handle });

			Backend.BindShaderVariable(genericParams2.HandleMVP, ref mvp);
			Backend.BindShaderVariable(genericParams2.HandleWorld, ref world);
			Backend.BindShaderVariable(genericParams2.HandleDiffuseTexture, 0);
			Backend.BindShaderVariable(genericParams2.HandleNormalMap, 1);

			foreach (var handle in wallsMesh.Handles)
			{
				Backend.DrawMesh(handle);
			}

			Backend.EndInstance();
			Backend.BeginInstance(shader.Handle, new int[] { floorDiffuse.Handle, floorNormalMap.Handle });

			foreach (var handle in ceilingMesh.Handles)
			{
				Backend.DrawMesh(handle);
			}

			foreach (var handle in floorMesh.Handles)
			{
				Backend.DrawMesh(handle);
			}

			Backend.EndInstance();
		}

		static void Main(string[] args)
		{
			using (var app = new Program())
			{
				app.Run();
			}
		}

		class PointLightParams
		{
			public int HandleMVP;
			public int HandleNormal;
			public int HandlePosition;

			public int HandleCameraPosition;

			public int HandleLightPositon;
			public int HandleLightColor;
			public int HandleLightRange;
			public int HandleScreenSize;
		}

		class FinalPassParams
		{
			public int HandleMVP;
			public int HandleDiffuse;
			public int HandleLight;
		}

		class GenericParams
		{
			public int HandleMVP;
			public int HandleWorld;
			public int HandleDiffuseTexture;
			public int HandleNormalMap;
		}

		class PointLight
		{
			public PointLight(Vector3 position, Vector3 color, float range)
			{
				Position = position;
				Color = color;
				Range = range;
			}

			public Vector3 Position;
			public Vector3 Color;
			public float Range;
		}

		class SpotLight
		{
		}
	}
}
