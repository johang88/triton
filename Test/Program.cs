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
			var deferredShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/deferred");

			var mesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/strike_trooper_armor");
			var mesh2 = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/strike_trooper_clothing");

			var wallsMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/walls");
			var floorMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/floor");
			var ceilingMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/ceiling");

			var skeleton = ResourceManager.Load<Triton.Graphics.SkeletalAnimation.Skeleton>("skeletons/strike_trooper_armor");
			var skeleton2 = ResourceManager.Load<Triton.Graphics.SkeletalAnimation.Skeleton>("skeletons/strike_trooper_clothing");

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

			var fullSceneRenderTarget = Backend.CreateRenderTarget("full_scene", Width, Height, Triton.Renderer.PixelInternalFormat.Rgba16f, 3, true);

			var batchBuffer = Backend.CreateBatchBuffer();

			batchBuffer.Begin();
			batchBuffer.AddQuad(new Vector2(-1, -1), new Vector2(2, 2), Vector2.Zero, new Vector2(1, 1));
			batchBuffer.End();

			while (!ResourceManager.AllResourcesLoaded())
			{
				Thread.Sleep(1);
			}

			var skeletonInstance = new Triton.Graphics.SkeletalAnimation.SkeletonInstance(skeleton);
			var skeletonInstance2 = new Triton.Graphics.SkeletalAnimation.SkeletonInstance(skeleton2);

			skeletonInstance.Play("run");
			skeletonInstance2.Play("run");

			var genericParams = new GenericParams();
			genericParams.HandleMVP = shaderSkinned.GetAliasedUniform("ModelViewProjection");
			genericParams.HandleWorld = shaderSkinned.GetAliasedUniform("World");
			genericParams.HandleDiffuseTexture = shaderSkinned.GetAliasedUniform("DiffuseTexture");
			genericParams.HandleNormalMap = shaderSkinned.GetAliasedUniform("NormalMap");
			genericParams.HandleBones = shaderSkinned.GetAliasedUniform("Bones");

			var genericParams2 = new GenericParams();
			genericParams2.HandleMVP = shader.GetAliasedUniform("ModelViewProjection");
			genericParams2.HandleWorld = shader.GetAliasedUniform("World");
			genericParams2.HandleDiffuseTexture = shader.GetAliasedUniform("DiffuseTexture");
			genericParams2.HandleNormalMap = shader.GetAliasedUniform("NormalMap");

			var deferredParams = new DeferredParams();
			deferredParams.HandleMVP = deferredShader.GetAliasedUniform("ModelViewProjection"); ;
			deferredParams.HandleDiffuse = deferredShader.GetAliasedUniform("DiffuseTexture");
			deferredParams.HandleNormal = deferredShader.GetAliasedUniform("NormalTexture");
			deferredParams.HandlePosition = deferredShader.GetAliasedUniform("PositionTexture");

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
				RenderCorridor(shader, wallsMesh, floorMesh, ceilingMesh, wallDiffuse, wallNormalMap, floorDiffuse, floorNormalMap, genericParams2, ref world, ref view, ref projection);

				// Render soldier
				world = Matrix4.CreateFromAxisAngle(Vector3.UnitY, (float)(Math.PI * (lightAlphaDir > 0.0f ? 2.0f : 1.0f))) * Matrix4.CreateTranslation(0, 0, lightDir.Z);

				var mvp = world * view * projection;

				Backend.BeginInstance(shaderSkinned.Handle, new int[] { texture.Handle, normalMap.Handle });

				Backend.BindShaderVariable(genericParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(genericParams.HandleWorld, ref world);
				Backend.BindShaderVariable(genericParams.HandleDiffuseTexture, 0);
				Backend.BindShaderVariable(genericParams.HandleNormalMap, 1);

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

				// Render tone mapped scene		
				Backend.BeginPass(null, new Vector4(0.25f, 0.5f, 0.75f, 1.0f));
				Backend.BeginInstance(deferredShader.Handle, new int[] { fullSceneRenderTarget.Textures[0].Handle, fullSceneRenderTarget.Textures[1].Handle, fullSceneRenderTarget.Textures[2].Handle });
				Backend.BindShaderVariable(deferredParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(deferredParams.HandleDiffuse, 0);
				Backend.BindShaderVariable(deferredParams.HandleNormal, 1);
				Backend.BindShaderVariable(deferredParams.HandlePosition, 2);

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

		class DeferredParams
		{
			public int HandleMVP;
			public int HandleDiffuse;
			public int HandleNormal;
			public int HandlePosition;
		}

		class GenericParams
		{
			public int HandleMVP;
			public int HandleWorld;
			public int HandleDiffuseTexture;
			public int HandleNormalMap;
			public int HandleBones;
		}
	}
}
