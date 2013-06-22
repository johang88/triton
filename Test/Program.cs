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
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.Console());
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.File("Logs/Test.txt"));

			WorkerThread = new WorkerThread();

			FileSystem = new Triton.Common.IO.FileSystem();
			ResourceManager = new Triton.Common.ResourceManager(WorkerThread.AddItem);

			FileSystem.AddPackage("FileSystem", "../data");
		}

		public void Dispose()
		{
			WorkerThread.Stop();
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
			var shader2 = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/test");
			var mesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/box");

			var texture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/test_d");
			var normalMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/test_n");

			var lightDir = new Vector3(3.95f, -0.64f, 0.5f);
			lightDir = lightDir.Normalize();
			var lightColor = new Vector3(1.2f, 1.1f, 1.15f);
			var ambientColor = new Vector3(0.35f, 0.35f, 0.4f);

			var renderTarget = Backend.CreateRenderTarget("test", 1280, 720, Triton.Renderer.PixelInternalFormat.Rgba16f, 1, true);
			var batchBuffer = Backend.CreateBatchBuffer();

			batchBuffer.Begin();
			batchBuffer.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			batchBuffer.End();

			while (!ResourceManager.AllResourcesLoaded() || !renderTarget.IsReady)
			{
				Thread.Sleep(1);
			}

			var genericParams = new GenericParams();
			genericParams.HandleMVP = shader.GetAliasedUniform("ModelViewProjection");
			genericParams.HandleWorld = shader.GetAliasedUniform("World");
			genericParams.HandleCameraPosition = shader.GetAliasedUniform("CameraPosition");
			genericParams.HandleLightDir = shader.GetAliasedUniform("LightDir");
			genericParams.HandleLightColor = shader.GetAliasedUniform("LightColor");
			genericParams.HandleAmbientColor = shader.GetAliasedUniform("AmbientColor");
			genericParams.HandleDiffuseTexture = shader.GetAliasedUniform("DiffuseTexture");
			genericParams.HandleNormalMap = shader.GetAliasedUniform("NormalMap");

			var toneMapParams = new ToneMapParams();
			toneMapParams.HandleMVP = shader2.GetAliasedUniform("ModelViewProjection"); ;
			toneMapParams.HandleDiffuse = shader2.GetAliasedUniform("DiffuseTexture");
			toneMapParams.HandleA = shader2.GetAliasedUniform("A");
			toneMapParams.HandleB = shader2.GetAliasedUniform("B");
			toneMapParams.HandleC = shader2.GetAliasedUniform("C");
			toneMapParams.HandleD = shader2.GetAliasedUniform("D");
			toneMapParams.HandleE = shader2.GetAliasedUniform("E");
			toneMapParams.HandleF = shader2.GetAliasedUniform("F");
			toneMapParams.HandleW = shader2.GetAliasedUniform("W");

			var angle = 0.0f;
			var cameraPos = new Vector3(0, 1.8f, 2);

			while (Running)
			{
				angle += 0.001f;
				var world = Matrix4.CreateRotationY(angle) * Matrix4.CreateTranslation(0, 0, 0.0f);
				var view = Matrix4.LookAt(cameraPos, Vector3.Zero, Vector3.UnitY);
				var projection = Matrix4.CreatePerspectiveFieldOfView(1.22173f, 1280.0f / 720.0f, 0.001f, 1000.0f);

				Matrix4 mvp = world * view * projection;

				Backend.BeginScene();

				Backend.BeginPass(renderTarget, new Vector4(0.5f, 0.5f, 0.6f, 1.0f));
				Backend.BeginInstance(shader.Handle, new int[] { texture.Handle, normalMap.Handle });

				Backend.BindShaderVariable(genericParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(genericParams.HandleWorld, ref world);
				Backend.BindShaderVariable(genericParams.HandleCameraPosition, ref cameraPos);

				Backend.BindShaderVariable(genericParams.HandleLightDir, ref lightDir);
				Backend.BindShaderVariable(genericParams.HandleLightColor, ref lightColor);
				Backend.BindShaderVariable(genericParams.HandleAmbientColor, ref ambientColor);

				Backend.BindShaderVariable(genericParams.HandleDiffuseTexture, 0);
				Backend.BindShaderVariable(genericParams.HandleNormalMap, 1);

				foreach (var handle in mesh.Handles)
				{
					Backend.DrawMesh(handle);
				}
				Backend.EndPass();

				mvp = Matrix4.Identity;

				Backend.BeginPass(null, new Vector4(0.25f, 0.5f, 0.75f, 1.0f));
				Backend.BeginInstance(shader2.Handle, new int[] { renderTarget.Textures[0].Handle });
				Backend.BindShaderVariable(toneMapParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(toneMapParams.HandleDiffuse, 0);

				Backend.BindShaderVariable(toneMapParams.HandleA, toneMapParams.A);
				Backend.BindShaderVariable(toneMapParams.HandleB, toneMapParams.B);
				Backend.BindShaderVariable(toneMapParams.HandleC, toneMapParams.C);
				Backend.BindShaderVariable(toneMapParams.HandleD, toneMapParams.D);
				Backend.BindShaderVariable(toneMapParams.HandleE, toneMapParams.E);
				Backend.BindShaderVariable(toneMapParams.HandleF, toneMapParams.F);
				Backend.BindShaderVariable(toneMapParams.HandleW, toneMapParams.W);

				Backend.DrawMesh(batchBuffer.Mesh.Handles[0]);
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

		class ToneMapParams
		{
			public int HandleMVP;
			public int HandleDiffuse;
			public int HandleA;
			public int HandleB;
			public int HandleC;
			public int HandleD;
			public int HandleE;
			public int HandleF;
			public int HandleW;

			public float A = 0.15f;
			public float B = 0.50f;
			public float C = 0.10f;
			public float D = 0.20f;
			public float E = 0.02f;
			public float F = 0.30f;
			public float W = 11.2f;
		}

		class GenericParams
		{
			public int HandleMVP;
			public int HandleWorld;
			public int HandleCameraPosition;
			public int HandleLightDir;
			public int HandleLightColor;
			public int HandleAmbientColor;
			public int HandleDiffuseTexture;
			public int HandleNormalMap;
		}
	}
}
