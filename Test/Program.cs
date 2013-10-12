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

			var shader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/generic");
			var pointLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/point_light");
			var spotLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/spot_light");
			var ambientLightShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/ambient_light");
			var finalPassShader = ResourceManager.Load<Triton.Graphics.Resources.ShaderProgram>("shaders/final");

			var unitSphere = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/unit_sphere");

			var wallsMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/walls");
			var floorMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/floor");
			var ceilingMesh = ResourceManager.Load<Triton.Graphics.Resources.Mesh>("models/ceiling");

			var wallMaterial = ResourceManager.Load<Triton.Graphics.Resources.Material>("materials/wall");
			var floorMaterial = ResourceManager.Load<Triton.Graphics.Resources.Material>("materials/floor");
			var ceilingMaterial = ResourceManager.Load<Triton.Graphics.Resources.Material>("materials/ceiling");

			var lightDir = new Vector3(0.0f, 2.0f, 0.0f);
			var lightColor = new Vector3(1.6f, 1.12f, 1.15f) * 2.0f;
			var ambientColor = new Vector3(0.2f, 0.2f, 0.3f);

			var lightStartPos = lightDir;
			var lightEndPos = lightDir + new Vector3(0, 0, 9.0f);
			var lightAlpha = 0.0f;
			var lightAlphaDir = 1.0f;

			var fullSceneRenderTarget = Backend.CreateRenderTarget("full_scene", Width, Height, Triton.Renderer.PixelInternalFormat.Rgba32f, 4, true);
			var lightAccumulationRenderTarget = Backend.CreateRenderTarget("light_accumulation", Width, Height, Triton.Renderer.PixelInternalFormat.Rgba32f, 2, true);

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
			genericParams2.HandleSpecularMap = shader.GetAliasedUniform("SpecularMap");

			var pointLightParams = new PointLightParams();
			pointLightParams.HandleMVP = pointLightShader.GetAliasedUniform("ModelViewProjection"); ;
			pointLightParams.HandleNormal = pointLightShader.GetAliasedUniform("NormalTexture");
			pointLightParams.HandlePosition = pointLightShader.GetAliasedUniform("PositionTexture");
			pointLightParams.HandleLightPositon = pointLightShader.GetAliasedUniform("LightPosition");
			pointLightParams.HandleCameraPosition = pointLightShader.GetAliasedUniform("CameraPosition");
			pointLightParams.HandleLightColor = pointLightShader.GetAliasedUniform("LightColor");
			pointLightParams.HandleLightRange = pointLightShader.GetAliasedUniform("LightRange");
			pointLightParams.HandleScreenSize = pointLightShader.GetAliasedUniform("ScreenSize");
			pointLightParams.HandleSpecular = pointLightShader.GetAliasedUniform("SpecularTexture");

			var spotLightParams = new SpotLightParams();
			spotLightParams.HandleMVP = spotLightShader.GetAliasedUniform("ModelViewProjection"); ;
			spotLightParams.HandleNormal = spotLightShader.GetAliasedUniform("NormalTexture");
			spotLightParams.HandlePosition = spotLightShader.GetAliasedUniform("PositionTexture");
			spotLightParams.HandleLightPositon = spotLightShader.GetAliasedUniform("LightPosition");
			spotLightParams.HandleCameraPosition = spotLightShader.GetAliasedUniform("CameraPosition");
			spotLightParams.HandleLightColor = spotLightShader.GetAliasedUniform("LightColor");
			spotLightParams.HandleLightRange = spotLightShader.GetAliasedUniform("LightRange");
			spotLightParams.HandleScreenSize = spotLightShader.GetAliasedUniform("ScreenSize");
			spotLightParams.HandleSpotLightParams = spotLightShader.GetAliasedUniform("SpotLightParams");
			spotLightParams.HandleDirection = spotLightShader.GetAliasedUniform("LightDirection");
			spotLightParams.HandleSpecular = spotLightShader.GetAliasedUniform("SpecularTexture");

			var ambientLightParams = new AmbientLightParams();
			ambientLightParams.HandleMVP = ambientLightShader.GetAliasedUniform("ModelViewProjection"); ;
			ambientLightParams.HandleNormal = ambientLightShader.GetAliasedUniform("NormalTexture");
			ambientLightParams.HandleAmbientColor = ambientLightShader.GetAliasedUniform("AmbientColor");

			var finalParams = new FinalPassParams();
			finalParams.HandleMVP = finalPassShader.GetAliasedUniform("ModelViewProjection"); ;
			finalParams.HandleDiffuse = finalPassShader.GetAliasedUniform("DiffuseTexture");
			finalParams.HandleLight = finalPassShader.GetAliasedUniform("LightTexture");
			finalParams.HandleSpecular = finalPassShader.GetAliasedUniform("SpecularTexture");

			var angle = 0.0f;
			var camera = new Camera(new Vector2(Width, Height));
			camera.Position.X = 5.0f;
			camera.Position.Z = 3.0f;
			camera.Position.Y = 1.5f;
			float cameraYaw = 0.0f, cameraPitch = 0.0f;

			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();

			var screenSize = new Vector2((float)Width, (float)Height);

			var pointLights = new List<PointLight>();
			var spotLights = new List<SpotLight>();
			var rng = new Random();

			var spotLight = new SpotLight(camera.Position, Vector3.UnitZ, new Vector3(1, 1, 1), 0.6f, 1.0f, 10.0f);
			spotLights.Add(spotLight);

			for (var i = 0; i < 100; i++)
			{
				var color = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
				var range = 1.0f;
				var offset = 0.5f;

				pointLights.Add(new PointLight(new Vector3(-0.5f, 0.5f, 1.0f + i * offset), color, range));
				pointLights.Add(new PointLight(new Vector3(0.5f, 0.5f, 1.0f + i * offset), color, range));

				pointLights.Add(new PointLight(new Vector3(-0.5f, 1.5f, 1.0f + i * offset), color, range));
				pointLights.Add(new PointLight(new Vector3(0.5f, 1.5f, 1.0f + i * offset), color, range));

				pointLights.Add(new PointLight(new Vector3(-0.5f, 2.5f, 1.0f + i * offset), color, range));
				pointLights.Add(new PointLight(new Vector3(0.5f, 2.5f, 1.0f + i * offset), color, range));
			}

			while (Running)
			{
				var deltaTime = (float)stopWatch.Elapsed.TotalSeconds;
				stopWatch.Restart();

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

				spotLight.Position = camera.Position;
				spotLight.Direction = Vector3.Transform(Vector3.UnitZ, camera.Orientation);

				angle += 1.4f * deltaTime;

				lightAlpha += deltaTime * 0.4f * lightAlphaDir;
				if (lightAlpha >= 1.0f)
					lightAlphaDir = -1.0f;
				else if (lightAlpha <= 0.0f)
					lightAlphaDir = 1.0f;

				lightDir = Vector3.Lerp(lightStartPos, lightEndPos, lightAlpha);

				foreach (var light in pointLights)
				{
					light.Position = Vector3.Lerp(light.StartPos, light.EndPos, lightAlpha);
				}

				Backend.BeginScene();

				// Render main scene
				Matrix4 view, projection;
				camera.GetViewMatrix(out view);
				camera.GetProjectionMatrix(out projection);

				Backend.BeginPass(fullSceneRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

				var world = Matrix4.Identity;
				RenderCorridor(shader, wallsMesh, floorMesh, ceilingMesh, wallMaterial, floorMaterial, ceilingMaterial, genericParams2, ref world, ref view, ref projection);

				Backend.EndInstance();

				Backend.EndPass();

				// Render lights
				Backend.BeginPass(lightAccumulationRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

				var mvp = Matrix4.Identity;

				// Ambient light
				Backend.BeginInstance(ambientLightShader.Handle, new int[] { fullSceneRenderTarget.Textures[1].Handle }, true, true, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One);
				Backend.BindShaderVariable(ambientLightParams.HandleNormal, 0);
				Backend.BindShaderVariable(ambientLightParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(ambientLightParams.HandleAmbientColor, ref ambientColor);

				Backend.DrawMesh(batchBuffer.Mesh.Handles[0]);

				// Point lights
				foreach (var light in pointLights)
				{
					var radius = light.Range;

					var cullFaceMode = Triton.Renderer.CullFaceMode.Back;
					var delta = light.Position - camera.Position;
					if (Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z) <= radius * radius)
					{
						cullFaceMode = Triton.Renderer.CullFaceMode.Front;
					}

					world = Matrix4.Scale(radius) * Matrix4.CreateTranslation(light.Position);
					mvp = world * view * projection;

					Backend.BeginInstance(pointLightShader.Handle, new int[] { fullSceneRenderTarget.Textures[1].Handle, fullSceneRenderTarget.Textures[2].Handle, fullSceneRenderTarget.Textures[3].Handle }, true, true, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, cullFaceMode);
					Backend.BindShaderVariable(pointLightParams.HandleNormal, 0);
					Backend.BindShaderVariable(pointLightParams.HandlePosition, 1);
					Backend.BindShaderVariable(pointLightParams.HandleSpecular, 2);
					Backend.BindShaderVariable(pointLightParams.HandleScreenSize, ref screenSize);

					Backend.BindShaderVariable(pointLightParams.HandleMVP, ref mvp);
					Backend.BindShaderVariable(pointLightParams.HandleLightPositon, ref light.Position);
					Backend.BindShaderVariable(pointLightParams.HandleLightColor, ref light.Color);
					Backend.BindShaderVariable(pointLightParams.HandleLightRange, light.Range);
					Backend.BindShaderVariable(pointLightParams.HandleCameraPosition, ref camera.Position);

					Backend.DrawMesh(unitSphere.Handles[0]);
				}

				// Spot lights
				foreach (var light in spotLights)
				{
					var radius = light.Range;

					//var cullFaceMode = Triton.Renderer.CullFaceMode.Back;
					//if (light.Intersects(camera))
					//{
					//	cullFaceMode = Triton.Renderer.CullFaceMode.Front;
					//}
					var cullFaceMode = Triton.Renderer.CullFaceMode.Back;
					var delta = light.Position - camera.Position;
					if (Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z) <= radius * radius)
					{
						cullFaceMode = Triton.Renderer.CullFaceMode.Front;
					}

					world = Matrix4.Scale(radius) * Matrix4.CreateTranslation(light.Position);
					mvp = world * view * projection;

					Backend.BeginInstance(spotLightShader.Handle, new int[] { fullSceneRenderTarget.Textures[1].Handle, fullSceneRenderTarget.Textures[2].Handle, fullSceneRenderTarget.Textures[3].Handle }, true, true, false, Triton.Renderer.BlendingFactorSrc.One, Triton.Renderer.BlendingFactorDest.One, cullFaceMode);
					Backend.BindShaderVariable(spotLightParams.HandleNormal, 0);
					Backend.BindShaderVariable(spotLightParams.HandlePosition, 1);
					Backend.BindShaderVariable(spotLightParams.HandleSpecular, 2);
					Backend.BindShaderVariable(spotLightParams.HandleScreenSize, ref screenSize);

					Backend.BindShaderVariable(spotLightParams.HandleMVP, ref mvp);
					Backend.BindShaderVariable(spotLightParams.HandleLightPositon, ref light.Position);
					Backend.BindShaderVariable(spotLightParams.HandleLightColor, ref light.Color);
					Backend.BindShaderVariable(spotLightParams.HandleLightRange, light.Range);
					Backend.BindShaderVariable(spotLightParams.HandleDirection, ref light.Direction);

					var spotParams = new Vector2((float)Math.Cos(light.InnerAngle / 2.0f), (float)Math.Cos(light.OuterAngle / 2.0f));
					Backend.BindShaderVariable(spotLightParams.HandleSpotLightParams, ref spotParams);
					Backend.BindShaderVariable(spotLightParams.HandleCameraPosition, ref camera.Position);

					Backend.DrawMesh(unitSphere.Handles[0]);
				}

				Backend.EndPass();

				mvp = Matrix4.Identity;

				// Render final scene	
				Backend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				Backend.BeginInstance(finalPassShader.Handle, new int[] { fullSceneRenderTarget.Textures[0].Handle, lightAccumulationRenderTarget.Textures[0].Handle, lightAccumulationRenderTarget.Textures[1].Handle });
				Backend.BindShaderVariable(finalParams.HandleMVP, ref mvp);
				Backend.BindShaderVariable(finalParams.HandleDiffuse, 0);
				Backend.BindShaderVariable(finalParams.HandleLight, 1);
				Backend.BindShaderVariable(finalParams.HandleSpecular, 2);

				Backend.DrawMesh(batchBuffer.Mesh.Handles[0]);
				Backend.EndPass();

				Backend.EndScene();

				Thread.Sleep(1);
			}
		}

		private void RenderCorridor(Triton.Graphics.Resources.ShaderProgram shader, Triton.Graphics.Resources.Mesh wallsMesh, Triton.Graphics.Resources.Mesh floorMesh, Triton.Graphics.Resources.Mesh ceilingMesh, Triton.Graphics.Resources.Material wallMaterial, Triton.Graphics.Resources.Material floorMaterial, Triton.Graphics.Resources.Material ceilingMaterial, GenericParams genericParams2, ref Matrix4 world, ref Matrix4 view, ref Matrix4 projection)
		{
			// Render corridor
			Matrix4 mvp = world * view * projection;
			Backend.BeginInstance(shader.Handle, new int[] { wallMaterial.Diffuse.Handle, wallMaterial.Normal.Handle, wallMaterial.Specular.Handle });

			Backend.BindShaderVariable(genericParams2.HandleMVP, ref mvp);
			Backend.BindShaderVariable(genericParams2.HandleWorld, ref world);
			Backend.BindShaderVariable(genericParams2.HandleDiffuseTexture, 0);
			Backend.BindShaderVariable(genericParams2.HandleNormalMap, 1);
			Backend.BindShaderVariable(genericParams2.HandleSpecularMap, 2);

			foreach (var handle in wallsMesh.Handles)
			{
				Backend.DrawMesh(handle);
			}

			Backend.EndInstance();

			Backend.BeginInstance(shader.Handle, new int[] { floorMaterial.Diffuse.Handle, floorMaterial.Normal.Handle, floorMaterial.Specular.Handle });

			foreach (var handle in floorMesh.Handles)
			{
				Backend.DrawMesh(handle);
			}
			Backend.EndInstance();

			Backend.BeginInstance(shader.Handle, new int[] { ceilingMaterial.Diffuse.Handle, ceilingMaterial.Normal.Handle, ceilingMaterial.Specular.Handle });
			foreach (var handle in ceilingMesh.Handles)
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

		class AmbientLightParams
		{
			public int HandleMVP;
			public int HandleNormal;

			public int HandleAmbientColor;
		}

		class PointLightParams
		{
			public int HandleMVP;
			public int HandleNormal;
			public int HandlePosition;
			public int HandleSpecular;

			public int HandleCameraPosition;

			public int HandleLightPositon;
			public int HandleLightColor;
			public int HandleLightRange;
			public int HandleScreenSize;
		}

		class SpotLightParams
		{
			public int HandleMVP;
			public int HandleNormal;
			public int HandlePosition;
			public int HandleSpecular;

			public int HandleDirection;

			public int HandleCameraPosition;

			public int HandleLightPositon;
			public int HandleLightColor;
			public int HandleLightRange;
			public int HandleScreenSize;
			public int HandleSpotLightParams;
		}

		class FinalPassParams
		{
			public int HandleMVP;
			public int HandleDiffuse;
			public int HandleLight;
			public int HandleSpecular;
		}

		class GenericParams
		{
			public int HandleMVP;
			public int HandleWorld;
			public int HandleDiffuseTexture;
			public int HandleNormalMap;
			public int HandleSpecularMap;
		}

		class PointLight
		{
			static Random RNG = new Random();

			public PointLight(Vector3 position, Vector3 color, float range)
			{
				EndPos = StartPos = Position = position;

				StartPos.X += 5.0f - (float)RNG.NextDouble() * 10.0f;
				StartPos.Y += 5.0f - (float)RNG.NextDouble() * 10.0f;
				StartPos.Z += 5.0f - (float)RNG.NextDouble() * 10.0f;

				EndPos.X += 5.0f - (float)RNG.NextDouble() * 10.0f;
				EndPos.Y += 5.0f - (float)RNG.NextDouble() * 10.0f;
				EndPos.Z += 5.0f - (float)RNG.NextDouble() * 10.0f;

				Color = color;
				Range = range;
			}

			public Vector3 StartPos;
			public Vector3 EndPos;

			public Vector3 Position;
			public Vector3 Color;
			public float Range;
		}

		class SpotLight
		{
			public SpotLight(Vector3 position, Vector3 direction, Vector3 color, float innerAngle, float outerAngle, float range)
			{
				Position = position;
				Color = color;
				Range = range;
				InnerAngle = innerAngle;
				OuterAngle = outerAngle;
				Direction = direction;
			}

			public Vector3 Position;
			public Vector3 Direction;
			public Vector3 Color;

			public float InnerAngle;
			public float OuterAngle;

			public float Range;

			public bool Intersects( Camera camera)
			{
				var lightPos = Position;
				var lightDir = Direction;
				var attAngle = OuterAngle;

				Vector3 clipRangeFix = -lightDir * (camera.NearClipDistance / (float)System.Math.Tan(attAngle / 2.0f));
				lightPos = lightPos + clipRangeFix;

				Vector3 lightToCamDir = camera.Position - lightPos;
				float distanceFromLight = lightToCamDir.Length;

				lightToCamDir = lightToCamDir.Normalize();

				var cosAngle = Vector3.Dot(lightToCamDir, lightDir);
				float angle = (float)Math.Acos(cosAngle);

				return (distanceFromLight <= (Range + clipRangeFix.Length)) && angle <= attAngle;
			}
		}
	}
}
