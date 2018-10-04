using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
	public class RendererImplementation
	{
		public readonly Graphics.Deferred.DeferredRenderer DeferredRenderer;
		public readonly Graphics.Post.PostEffectManager PostEffectManager;

		private readonly Graphics.Backend GraphicsBackend;

		private readonly Graphics.SpriteBatch SpriteRenderer;

		/// <summary>
		/// Custom render tasks, done after scene processing
		/// </summary>
		public readonly TaskList Tasks = new TaskList();

		public RendererImplementation(Common.IO.FileSystem fileSystem, Common.ResourceManager resourceManager, Graphics.Backend backend)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (backend == null)
				throw new ArgumentNullException("backend");

			GraphicsBackend = backend;

			DeferredRenderer = new Graphics.Deferred.DeferredRenderer(resourceManager, GraphicsBackend, GraphicsBackend.Width, GraphicsBackend.Height);
			PostEffectManager = new Graphics.Post.PostEffectManager(fileSystem, resourceManager, GraphicsBackend, GraphicsBackend.Width, GraphicsBackend.Height);

			SpriteRenderer = GraphicsBackend.CreateSpriteBatch();
		}

		/// <summary>
		/// Issue rendering commands to render a single frame
		/// </summary>
		public void Render(Graphics.Stage stage, Graphics.Camera camera, float deltaTime)
		{
			GraphicsBackend.BeginScene();

			// Render the complete scene
			var lightOutput = DeferredRenderer.Render(stage, camera);
			var postProcessedResult = PostEffectManager.Render(camera, stage, DeferredRenderer.GBuffer, lightOutput, deltaTime);

			// Blit renderer output to window
			GraphicsBackend.BeginPass(null, Vector4.Zero, Renderer.ClearFlags.Color);

			SpriteRenderer.RenderQuad(postProcessedResult.Textures[0], Vector2.Zero, new Vector2(GraphicsBackend.Width, GraphicsBackend.Height));
			SpriteRenderer.Render(GraphicsBackend.Width, GraphicsBackend.Height);

			// Process any additional tasks
			Tasks.Execute(deltaTime);

			// Swap command buffers
			GraphicsBackend.EndScene();
		}
	}
}
