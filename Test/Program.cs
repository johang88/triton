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
			var tonemapShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/tonemap");
			var highpassShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/highpass");
			var blur1Shader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/blur1");
			var blur2Shader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/blur2");

			var mesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/strike_trooper_armor");
			var mesh2 = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/strike_trooper_clothing");
			var mesh3 = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/strike_trooper_visor");

			var texture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/strike_trooper_d");
			var normalMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/strike_trooper_n");
			var specularMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/strike_trooper_s");

			var lightDir = new Vector3(3.95f, -0.94f, 0.5f);
			lightDir = lightDir.Normalize();
			var lightColor = new Vector3(1.2f, 1.12f, 1.15f);
			var ambientColor = new Vector3(0.75f, 0.75f, 0.75f);
			var finalLightColor = lightColor;

			var lightIntensity = 1.0f;

			var fullSceneRenderTarget = Backend.CreateRenderTarget("full_scene", 1280, 720, Triton.Renderer.PixelInternalFormat.Rgba16f, 1, true);

			var blurRenderTargetSize = new Vector2(1280 / 4.0f, 720 / 4.0f);
			var blur1RenderTarget = Backend.CreateRenderTarget("blur1", 1280 / 4, 720 / 4, Triton.Renderer.PixelInternalFormat.Rgba16f, 1, true);
			var blur2RenderTarget = Backend.CreateRenderTarget("blur2", 1280 / 4, 720 / 4, Triton.Renderer.PixelInternalFormat.Rgba16f, 1, true);

			var renderTargets = new Triton.Graphics.RenderTarget[] { fullSceneRenderTarget, blur1RenderTarget, blur2RenderTarget };

			var batchBuffer = Backend.CreateBatchBuffer();

			batchBuffer.Begin();
			batchBuffer.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			batchBuffer.End();

			while (!ResourceManager.AllResourcesLoaded() || !renderTargets.All(r => r.IsReady))
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
			genericParams.HandleSpecularMap = shader.GetAliasedUniform("SpecularMap");

			var toneMapParams = new ToneMapParams();
			toneMapParams.HandleMVP = tonemapShader.GetAliasedUniform("ModelViewProjection"); ;
			toneMapParams.HandleDiffuse = tonemapShader.GetAliasedUniform("DiffuseTexture");
			toneMapParams.HandleBlur = tonemapShader.GetAliasedUniform("BlurTexture");
			toneMapParams.HandleA = tonemapShader.GetAliasedUniform("A");
			toneMapParams.HandleB = tonemapShader.GetAliasedUniform("B");
			toneMapParams.HandleC = tonemapShader.GetAliasedUniform("C");
			toneMapParams.HandleD = tonemapShader.GetAliasedUniform("D");
			toneMapParams.HandleE = tonemapShader.GetAliasedUniform("E");
			toneMapParams.HandleF = tonemapShader.GetAliasedUniform("F");
			toneMapParams.HandleW = tonemapShader.GetAliasedUniform("W");

			var highPassParams = new HighPassParams();
			highPassParams.HandleMVP = highpassShader.GetAliasedUniform("ModelViewProjection");
			highPassParams.HandleDiffuseTexture = highpassShader.GetAliasedUniform("DiffuseTexture");

			var blur1Params = new BlurParams();
			blur1Params.HandleMVP = blur1Shader.GetAliasedUniform("ModelViewProjection");
			blur1Params.HandleDiffuseTexture = blur1Shader.GetAliasedUniform("DiffuseTexture");
			blur1Params.TexelSize = blur1Shader.GetAliasedUniform("TexelSize");

			var blur2Params = new BlurParams();
			blur2Params.HandleMVP = blur2Shader.GetAliasedUniform("ModelViewProjection");
			blur2Params.HandleDiffuseTexture = blur2Shader.GetAliasedUniform("DiffuseTexture");
			blur2Params.TexelSize = blur2Shader.GetAliasedUniform("TexelSize");

			var angle = 0.0f;
			var cameraPos = new Vector3(0, 1.8f, 200);

			var lightIntensityDir = 1.0f;

			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();

			while (Running)
			{
				var deltaTime = (float)stopWatch.Elapsed.TotalSeconds;
				stopWatch.Restart();

				angle += 1.4f * deltaTime;

				var world = Matrix4.CreateRotationX((float)(Math.PI / 2.0f)) * Matrix4.CreateRotationY(angle) * Matrix4.CreateTranslation(0, -100, 0.0f);
				var view = Matrix4.LookAt(cameraPos, Vector3.Zero, Vector3.UnitY);
				var projection = Matrix4.CreatePerspectiveFieldOfView(1.22173f, 1280.0f / 720.0f, 0.001f, 1000.0f);

				Matrix4 mvp = world * view * projection;

				var targetIntensity = lightIntensityDir > 0.0f ? 2.0f : 0.2f;
				if (lightIntensityDir > 0.0f && lightIntensity >= 1.9f)
				{
					lightIntensityDir = -1.0f;
				}
				else if (lightIntensityDir < 0.0f && lightIntensity <= 0.55f)
				{
					lightIntensityDir = 1.0f;
				}

				lightIntensity = lightIntensity + 2.0f * deltaTime * (targetIntensity - lightIntensity);
				finalLightColor = lightColor * lightIntensity * 2;

				Backend.BeginScene();

				// Render main scene
				Backend.BeginPass(fullSceneRenderTarget, new Vector4(0.5f, 0.5f, 0.6f, 1.0f));
				Backend.BeginInstance(shader.Handle, new int[] { texture.Handle, normalMap.Handle, specularMap.Handle });

				Backend.BindShaderVariable(genericParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(genericParams.HandleWorld, ref world);
				Backend.BindShaderVariable(genericParams.HandleCameraPosition, ref cameraPos);

				Backend.BindShaderVariable(genericParams.HandleLightDir, ref lightDir);
				Backend.BindShaderVariable(genericParams.HandleLightColor, ref finalLightColor);
				Backend.BindShaderVariable(genericParams.HandleAmbientColor, ref ambientColor);

				Backend.BindShaderVariable(genericParams.HandleDiffuseTexture, 0);
				Backend.BindShaderVariable(genericParams.HandleNormalMap, 1);
				Backend.BindShaderVariable(genericParams.HandleSpecularMap, 2);

				foreach (var handle in mesh.Handles)
				{
					Backend.DrawMesh(handle);
				}

				foreach (var handle in mesh2.Handles)
				{
					Backend.DrawMesh(handle);
				}

				foreach (var handle in mesh3.Handles)
				{
					Backend.DrawMesh(handle);
				}

				Backend.EndPass();

				mvp = Matrix4.Identity;

				// Render high pass
				Backend.BeginPass(blur1RenderTarget, new Vector4(0.25f, 0.5f, 0.75f, 1.0f));
				Backend.BeginInstance(highpassShader.Handle, new int[] { fullSceneRenderTarget	.Textures[0].Handle });
				Backend.BindShaderVariable(highPassParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(highPassParams.HandleDiffuseTexture, 0);

				Backend.DrawMesh(batchBuffer.Mesh.Handles[0]);
				Backend.EndPass();

				// Render blur 1
				Backend.BeginPass(blur2RenderTarget, new Vector4(0.25f, 0.5f, 0.75f, 1.0f));
				Backend.BeginInstance(blur1Shader.Handle, new int[] { blur1RenderTarget.Textures[0].Handle });
				Backend.BindShaderVariable(blur1Params.HandleMVP, ref mvp);
				Backend.BindShaderVariable(blur1Params.HandleDiffuseTexture, 0);
				Backend.BindShaderVariable(blur1Params.TexelSize, 1.0f / blurRenderTargetSize.X);

				Backend.DrawMesh(batchBuffer.Mesh.Handles[0]);
				Backend.EndPass();

				// Render blur 2
				Backend.BeginPass(blur1RenderTarget, new Vector4(0.25f, 0.5f, 0.75f, 1.0f));
				Backend.BeginInstance(blur2Shader.Handle, new int[] { blur2RenderTarget.Textures[0].Handle });
				Backend.BindShaderVariable(blur2Params.HandleMVP, ref mvp);
				Backend.BindShaderVariable(blur2Params.HandleDiffuseTexture, 0);
				Backend.BindShaderVariable(blur2Params.TexelSize, 1.0f / blurRenderTargetSize.Y);

				Backend.DrawMesh(batchBuffer.Mesh.Handles[0]);
				Backend.EndPass();

				// Render tone mapped scene
				Backend.BeginPass(null, new Vector4(0.25f, 0.5f, 0.75f, 1.0f));
				Backend.BeginInstance(tonemapShader.Handle, new int[] { fullSceneRenderTarget.Textures[0].Handle, blur1RenderTarget .Textures[0].Handle});
				Backend.BindShaderVariable(toneMapParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(toneMapParams.HandleDiffuse, 0);
				Backend.BindShaderVariable(toneMapParams.HandleBlur, 1);

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
			public int HandleBlur;
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
			public int HandleSpecularMap;
		}

		class HighPassParams
		{
			public int HandleMVP;
			public int HandleDiffuseTexture;
		}

		class BlurParams
		{
			public int HandleMVP;
			public int HandleDiffuseTexture;
			public int TexelSize;
		}
	}
}
