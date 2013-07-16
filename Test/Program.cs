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

			var shaderSkinned = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/generic_skinned");
			var shader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/generic");
			var tonemapShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/tonemap");
			var highpassShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/highpass");
			var blur1Shader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/blur1");
			var blur2Shader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/blur2");

			var mesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/strike_trooper_armor");
			var mesh2 = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/strike_trooper_clothing");
			var mesh3 = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/assault_gun");
			var mesh4 = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/strike_trooper_visor");

			var wallsMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/walls");
			var floorMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/floor");
			var ceilingMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/ceiling");

			var skeleton = ResourceManager.Load<Triton.Graphics.SkeletalAnimation.Skeleton>("skeletons/strike_trooper_armor");
			var skeleton2 = ResourceManager.Load<Triton.Graphics.SkeletalAnimation.Skeleton>("skeletons/strike_trooper_clothing");
			var skeleton3 = ResourceManager.Load<Triton.Graphics.SkeletalAnimation.Skeleton>("skeletons/assault_gun");
			var skeleton4 = ResourceManager.Load<Triton.Graphics.SkeletalAnimation.Skeleton>("skeletons/strike_trooper_visor");

			var texture = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/strike_trooper_d");
			var normalMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/strike_trooper_n");
			var specularMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/strike_trooper_s");

			var wallDiffuse = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/wall_d");
			var wallNormalMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/wall_n");
			var wallSpecularMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/wall_s");

			var floorDiffuse = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/floor_d");
			var floorNormalMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/floor_n");
			var floorSpecularMap = ResourceManager.Load<Triton.Graphics.Resources.Texture>("textures/floor_s");

			var lightDir = new Vector3(0.0f, 2.0f, 0.0f);
			var lightColor = new Vector3(1.6f, 1.12f, 1.15f) * 2.0f;
			var ambientColor = new Vector3(0.2f, 0.2f, 0.3f);

			var lightStartPos = lightDir;
			var lightEndPos = lightDir + new Vector3(0, 0, 9.0f);
			var lightAlpha = 0.0f;
			var lightAlphaDir = 1.0f;

			var fullSceneRenderTarget = Backend.CreateRenderTarget("full_scene", Width, Height, Triton.Renderer.PixelInternalFormat.Rgba16f, 1, true);

			var blurRenderTargetSize = new Vector2(Width / 4.0f, Height / 4.0f);
			var blur1RenderTarget = Backend.CreateRenderTarget("blur1", Width / 4, Height/ 4, Triton.Renderer.PixelInternalFormat.Rgba16f, 1, true);
			var blur2RenderTarget = Backend.CreateRenderTarget("blur2", Width / 4, Height / 4, Triton.Renderer.PixelInternalFormat.Rgba16f, 1, true);

			var renderTargets = new Triton.Graphics.RenderTarget[] { fullSceneRenderTarget, blur1RenderTarget, blur2RenderTarget };

			var batchBuffer = Backend.CreateBatchBuffer();

			batchBuffer.Begin();
			batchBuffer.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			batchBuffer.End();

			while (!ResourceManager.AllResourcesLoaded() || !renderTargets.All(r => r.IsReady))
			{
				Thread.Sleep(1);
			}

			var skeletonInstance = new Triton.Graphics.SkeletalAnimation.SkeletonInstance(skeleton);
			var skeletonInstance2 = new Triton.Graphics.SkeletalAnimation.SkeletonInstance(skeleton2);
			var skeletonInstance3 = new Triton.Graphics.SkeletalAnimation.SkeletonInstance(skeleton3);
			var skeletonInstance4 = new Triton.Graphics.SkeletalAnimation.SkeletonInstance(skeleton4);

			skeletonInstance.Play("run");
			skeletonInstance2.Play("run");
			skeletonInstance3.Play("run");
			skeletonInstance4.Play("run");

			var genericParams = new GenericParams();
			genericParams.HandleMVP = shaderSkinned.GetAliasedUniform("ModelViewProjection");
			genericParams.HandleWorld = shaderSkinned.GetAliasedUniform("World");
			genericParams.HandleCameraPosition = shaderSkinned.GetAliasedUniform("CameraPosition");
			genericParams.HandleLightDir = shaderSkinned.GetAliasedUniform("LightDir");
			genericParams.HandleLightColor = shaderSkinned.GetAliasedUniform("LightColor");
			genericParams.HandleAmbientColor = shaderSkinned.GetAliasedUniform("AmbientColor");
			genericParams.HandleDiffuseTexture = shaderSkinned.GetAliasedUniform("DiffuseTexture");
			genericParams.HandleNormalMap = shaderSkinned.GetAliasedUniform("NormalMap");
			genericParams.HandleSpecularMap = shaderSkinned.GetAliasedUniform("SpecularMap");
			genericParams.HandleBones = shaderSkinned.GetAliasedUniform("Bones");

			var genericParams2 = new GenericParams();
			genericParams2.HandleMVP = shader.GetAliasedUniform("ModelViewProjection");
			genericParams2.HandleWorld = shader.GetAliasedUniform("World");
			genericParams2.HandleCameraPosition = shader.GetAliasedUniform("CameraPosition");
			genericParams2.HandleLightDir = shader.GetAliasedUniform("LightDir");
			genericParams2.HandleLightColor = shader.GetAliasedUniform("LightColor");
			genericParams2.HandleAmbientColor = shader.GetAliasedUniform("AmbientColor");
			genericParams2.HandleDiffuseTexture = shader.GetAliasedUniform("DiffuseTexture");
			genericParams2.HandleNormalMap = shader.GetAliasedUniform("NormalMap");
			genericParams2.HandleSpecularMap = shader.GetAliasedUniform("SpecularMap");

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
			var cameraPos = new Vector3(0, 1.8f, -2.5f);

			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();

			while (Running)
			{
				var deltaTime = (float)stopWatch.Elapsed.TotalSeconds;
				stopWatch.Restart();

				skeletonInstance.Update(deltaTime);
				skeletonInstance2.Update(deltaTime);

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


				Backend.BeginPass(fullSceneRenderTarget, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

				var world = Matrix4.CreateFromAxisAngle(Vector3.UnitX, (float)(Math.PI / 2.0)) * Matrix4.CreateTranslation(0, 5f, 12.0f);
				RenderCorridor(shader, wallsMesh, floorMesh, ceilingMesh, wallDiffuse, wallNormalMap, wallSpecularMap, floorDiffuse, floorNormalMap, floorSpecularMap, ref lightDir, ref ambientColor, ref lightColor, genericParams2, ref cameraPos, ref world, ref view, ref projection);

				// Render soldier
				world = Matrix4.CreateFromAxisAngle(Vector3.UnitY, (float)(Math.PI * (lightAlphaDir > 0.0f ? 2.0f : 1.0f))) * Matrix4.CreateTranslation(0, 0, lightDir.Z);

				var mvp = world * view * projection;

				Backend.BeginInstance(shaderSkinned.Handle, new int[] { texture.Handle, normalMap.Handle, specularMap.Handle });

				Backend.BindShaderVariable(genericParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(genericParams.HandleWorld, ref world);
				Backend.BindShaderVariable(genericParams.HandleCameraPosition, ref cameraPos);

				Backend.BindShaderVariable(genericParams.HandleLightDir, ref lightDir);
				Backend.BindShaderVariable(genericParams.HandleLightColor, ref lightColor);
				Backend.BindShaderVariable(genericParams.HandleAmbientColor, ref ambientColor);

				Backend.BindShaderVariable(genericParams.HandleDiffuseTexture, 0);
				Backend.BindShaderVariable(genericParams.HandleNormalMap, 1);
				Backend.BindShaderVariable(genericParams.HandleSpecularMap, 2);

				Backend.BindShaderVariable(genericParams.HandleBones, ref skeletonInstance.FinalBoneTransforms);
				foreach (var handle in mesh.Handles)
				{
					Backend.DrawMesh(handle);
				}

				Backend.BindShaderVariable(genericParams.HandleBones, ref skeletonInstance2.FinalBoneTransforms);
				foreach (var handle in mesh2.Handles)
				{
					Backend.DrawMesh(handle);
				}

				Backend.EndInstance();

				Backend.EndPass();

				mvp = Matrix4.Identity;

				// Render high pass
				Backend.BeginPass(blur1RenderTarget, new Vector4(0.25f, 0.5f, 0.75f, 1.0f));
				Backend.BeginInstance(highpassShader.Handle, new int[] { fullSceneRenderTarget.Textures[0].Handle });
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
				Backend.BeginInstance(tonemapShader.Handle, new int[] { fullSceneRenderTarget.Textures[0].Handle, blur1RenderTarget.Textures[0].Handle });
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

		private void RenderCorridor(Triton.Graphics.Resources.ShaderProgram shader, Triton.Graphics.Resources.Mesh wallsMesh, Triton.Graphics.Resources.Mesh floorMesh, Triton.Graphics.Resources.Mesh ceilingMesh, Triton.Graphics.Resources.Texture wallDiffuse, Triton.Graphics.Resources.Texture wallNormalMap, Triton.Graphics.Resources.Texture wallSpecularMap, Triton.Graphics.Resources.Texture floorDiffuse, Triton.Graphics.Resources.Texture floorNormalMap, Triton.Graphics.Resources.Texture floorSpecularMap, ref Vector3 lightDir, ref Vector3 ambientColor, ref Vector3 finalLightColor, GenericParams genericParams2, ref Vector3 cameraPos, ref Matrix4 world, ref Matrix4 view, ref Matrix4 projection)
		{
			// Render corridor
			Matrix4 mvp = world * view * projection;
			Backend.BeginInstance(shader.Handle, new int[] { wallDiffuse.Handle, wallNormalMap.Handle, wallSpecularMap.Handle });

			Backend.BindShaderVariable(genericParams2.HandleMVP, ref mvp);
			Backend.BindShaderVariable(genericParams2.HandleWorld, ref world);
			Backend.BindShaderVariable(genericParams2.HandleCameraPosition, ref cameraPos);

			Backend.BindShaderVariable(genericParams2.HandleLightDir, ref lightDir);
			Backend.BindShaderVariable(genericParams2.HandleLightColor, ref finalLightColor);
			Backend.BindShaderVariable(genericParams2.HandleAmbientColor, ref ambientColor);

			Backend.BindShaderVariable(genericParams2.HandleDiffuseTexture, 0);
			Backend.BindShaderVariable(genericParams2.HandleNormalMap, 1);
			Backend.BindShaderVariable(genericParams2.HandleSpecularMap, 2);

			foreach (var handle in wallsMesh.Handles)
			{
				Backend.DrawMesh(handle);
			}

			Backend.EndInstance();
			Backend.BeginInstance(shader.Handle, new int[] { floorDiffuse.Handle, floorNormalMap.Handle, floorSpecularMap.Handle });

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
			public int HandleBones;
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
