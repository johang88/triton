using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;
using System.Threading;

namespace Triton.Game
{
	public abstract class Game : IDisposable
	{
		public Triton.Graphics.Backend GraphicsBackend { get; private set; }
		public Triton.Common.IO.FileSystem FileSystem { get; private set; }
		public Triton.Common.ResourceManager ResourceManager { get; private set; }

		private Thread UpdateThread { get; set; }

		public bool Running { get; set; }

		private ManualResetEvent RendererReady = new ManualResetEvent(false);

		public int RequestedWidth { get; set; }
		public int RequestedHeight { get; set; }
		public float ResolutionScale { get; set; }

		public Graphics.Stage Stage { get; private set; }
		public Graphics.Camera Camera { get; private set; }

		public Input.InputManager InputManager { get; private set; }

		public Graphics.Deferred.DeferredRenderer DeferredRenderer { get; private set; }
		public Graphics.Post.PostEffectManager PostEffectManager { get; private set; }

		public Triton.Physics.World PhysicsWorld { get; private set; }

		public float PhysicsStepSize = 1.0f / 100.0f;

		public Audio.AudioSystem AudioSystem { get; private set; }

		public World.GameWorld GameWorld;

		public Graphics.SpriteBatch SpriteRenderer;
		public Graphics.Resources.BitmapFont DebugFont;

		public DebugFlags DebugFlags;

		private long FrameCount = 0;
		private float FrameTime = 0.0f;
		protected float ElapsedTime = 0.0f;

		private readonly string Name;

		public Game(string name, string logPath = "logs/")
		{
			Name = name;
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.Console());
			Triton.Common.Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.File(string.Format("{0}/{1}.txt", logPath, name)));

			FileSystem = new Common.IO.FileSystem(MountFileSystem());
			ResourceManager = new Triton.Common.ResourceManager(FileSystem);

			ResolutionScale = 1.0f; // This is default for obvious reasons
		}

		public virtual void Dispose()
		{
			AudioSystem.Dispose();
			ResourceManager.Dispose();
		}

		public void Run()
		{
			Running = true;

			UpdateThread = new Thread(UpdateLoop);
			UpdateThread.Name = "Update Thread";
			UpdateThread.Start();

			RenderLoop();
		}

		private void RenderLoop()
		{
			using (GraphicsBackend = new Triton.Graphics.Backend(ResourceManager, RequestedWidth, RequestedHeight, ResolutionScale, Name, false))
			{
				Triton.Graphics.Resources.ResourceLoaders.Init(ResourceManager, GraphicsBackend, FileSystem);

				RendererReady.Set();

				while (Running)
				{
					ResourceManager.TickResourceLoading(100);
					
					if (!GraphicsBackend.Process())
						break;

					Thread.Sleep(1);
				}

				Running = false;
			}
		}

		void UpdateLoop()
		{
			WaitHandle.WaitAll(new WaitHandle[] { RendererReady });

			Physics.Resources.ResourceLoaders.Init(ResourceManager, FileSystem);

			AudioSystem = new Audio.AudioSystem(FileSystem);
			PhysicsWorld = new Triton.Physics.World(GraphicsBackend, ResourceManager);

			DeferredRenderer = new Graphics.Deferred.DeferredRenderer(ResourceManager, GraphicsBackend, GraphicsBackend.Width, GraphicsBackend.Height);
			PostEffectManager = new Graphics.Post.PostEffectManager(ResourceManager, GraphicsBackend, GraphicsBackend.Width, GraphicsBackend.Height);

			Stage = new Graphics.Stage(ResourceManager);
			Camera = new Graphics.Camera(new Vector2(GraphicsBackend.Width, GraphicsBackend.Height));

			InputManager = new Input.InputManager(GraphicsBackend.WindowBounds);

			GameWorld = new World.GameWorld(Stage, InputManager, ResourceManager, PhysicsWorld, Camera);

			LoadResources();

			// Wait until all initial resources have been loaded
			while (!ResourceManager.AllResourcesLoaded())
			{
				Thread.Sleep(1);
			}

			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();

			var accumulator = 0.0f;

			while (Running)
			{
				FrameCount++;
				FrameTime = (float)watch.Elapsed.TotalSeconds;
				watch.Restart();

				ElapsedTime += FrameTime;

				if (GraphicsBackend.HasFocus)
				{
					InputManager.Update();
				}

				accumulator += FrameTime;
				while (accumulator >= PhysicsStepSize)
				{
					PhysicsWorld.Update(PhysicsStepSize);
					accumulator -= PhysicsStepSize;
				}

				AudioSystem.Update();

				GameWorld.Update(FrameTime);

				Update(FrameTime);

				RenderScene(FrameTime);
				Thread.Sleep(1);
			}
		}

		/// <summary>
		/// Feed render commands to the graphics backend.
		/// Only override this method if you wish to customize the rendering pipeline.
		/// </summary>
		protected virtual void RenderScene(float deltaTime)
		{
			GraphicsBackend.BeginScene();

			var lightOutput = DeferredRenderer.Render(Stage, Camera);

			var postProcessedResult = PostEffectManager.Render(Camera, DeferredRenderer.GBuffer, lightOutput, deltaTime);

			GraphicsBackend.BeginPass(null, Vector4.Zero, false);

			SpriteRenderer.RenderQuad(postProcessedResult.Textures[0], Vector2.Zero, new Vector2(GraphicsBackend.WindowWidth, GraphicsBackend.WindowHeight));
			SpriteRenderer.Render(GraphicsBackend.WindowWidth, GraphicsBackend.WindowHeight);

			RenderUI(deltaTime);

			if ((DebugFlags & DebugFlags.Physics) == DebugFlags.Physics)
			{
				GraphicsBackend.BeginPass(null, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
				PhysicsWorld.DrawDebugInfo(Camera);
				GraphicsBackend.EndPass();
			}

			var debugYOffset = 0;
			if ((DebugFlags & DebugFlags.GBuffer) == DebugFlags.GBuffer)
			{
				var textures = DeferredRenderer.GBuffer.Textures;
				for (var i = 0; i < textures.Length; i++)
				{
					SpriteRenderer.RenderQuad(textures[i], new Vector2(257 * i + 1, 1 + (257 * debugYOffset)), new Vector2(256, 256), Vector2.Zero, Vector2.One, Vector4.One, false, i == 0);
				}

				SpriteRenderer.Render(GraphicsBackend.WindowWidth, GraphicsBackend.WindowHeight);
				debugYOffset++;
			}

			if ((DebugFlags & DebugFlags.ShadowMaps) == DebugFlags.ShadowMaps)
			{
				var textures = DeferredRenderer.DirectionalShadowsRenderTarget.Select(r => r.Textures[0]).ToArray();
				for (var i = 0; i < textures.Length; i++)
				{
					SpriteRenderer.RenderQuad(textures[i], new Vector2(257 * i + 1, 1 + (257 * debugYOffset)), new Vector2(256, 256), Vector2.Zero, Vector2.One, Vector4.One, false, false);
				}

				SpriteRenderer.Render(GraphicsBackend.WindowWidth, GraphicsBackend.WindowHeight);
			}

			if ((DebugFlags & Triton.Game.DebugFlags.RenderStats) == Triton.Game.DebugFlags.RenderStats)
			{
				var averageFPS = FrameCount / ElapsedTime;
				var fps = 1.0f / FrameTime;

				var allocatedMemory = GC.GetTotalMemory(false) / 1024 / 1024;

				var offsetY = 1;

				DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4), Vector4.One, "Frame stats:");
				DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tFrame time: {0:0.00}ms", GraphicsBackend.FrameTime * 1000.0f);
				DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tAverage FPS: {0:0}", averageFPS);
				DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tFPS: {0:0}", fps);
				DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tLights: {0}", DeferredRenderer.RenderedLights);
				DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "GC stats:");
				DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tAllocated memory {0}mb", allocatedMemory);

				DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\tCollection count");
				for (var i = 0; i < GC.MaxGeneration; i++)
				{
					DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\t\tGen ({0})\t{1}", i, GC.CollectionCount(i));
				}

				DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "Profiler");
				Graphics.Profiler.ProfilerSection[] sections;
				int sectionCount;
				GraphicsBackend.SecondaryProfiler.GetSections(out sections, out sectionCount);
				for (var i = 0; i < sectionCount; i++)
				{
					var section = sections[i];
					var diff = section.End - section.Start;
					var name = Common.HashedStringTable.GetString(new Common.HashedString(section.Name));

					DebugFont.DrawText(SpriteRenderer, new Vector2(4, 4 + DebugFont.LineHeight * offsetY++), Vector4.One, "\t{0} {1}ms", name, diff);
				}

				SpriteRenderer.Render(GraphicsBackend.WindowWidth, GraphicsBackend.WindowHeight);
			}

			GraphicsBackend.EndScene();
		}

		protected virtual void RenderUI(float deltaTime)
		{

		}

		/// <summary>
		/// Mount packages to the file system
		/// <see cref="Triton.Common.IO.FileSystem.AddPackage"/>
		/// </summary>
		protected abstract SharpFileSystem.IFileSystem MountFileSystem();

		/// <summary>
		/// Preload resources before the main loop is started
		/// </summary>
		protected virtual void LoadResources()
		{
			DebugFont = ResourceManager.Load<Triton.Graphics.Resources.BitmapFont>("/fonts/system_font");
			SpriteRenderer = GraphicsBackend.CreateSpriteBatch();
		}

		/// <summary>
		/// Update the game
		/// </summary>
		/// <param name="frameTime"></param>
		protected virtual void Update(float frameTime)
		{
		}
	}
}
