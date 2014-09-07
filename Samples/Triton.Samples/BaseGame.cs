using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFileSystem.FileSystems;
using SharpFileSystem;

namespace Triton.Samples
{
	public abstract class BaseGame : Triton.Game.Game
	{
		protected Random RNG = new Random();

		public BaseGame(string name)
			: base(name)
		{
			RequestedWidth = 1280;
			RequestedHeight = 720;
		}

		protected KeyValuePair<FileSystemPath, IFileSystem> CreateMount(string mountPoint, IFileSystem fileSystem)
		{
			return new KeyValuePair<FileSystemPath, IFileSystem>(FileSystemPath.Parse(mountPoint), fileSystem);
		}

		protected override SharpFileSystem.IFileSystem MountFileSystem()
		{
			return new FileSystemMounter(
				CreateMount("/tmp/", new PhysicalFileSystem("./tmp")),
				CreateMount("/", new MergedFileSystem(
					new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/core_data/")),
					new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/samples_data/")),
					new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/no_dist/")),
					new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/generated/"))
					))
				);
		}

		protected override void Update(float frameTime)
		{
			base.Update(frameTime);

			if (InputManager.WasKeyPressed(Input.Key.F))
			{
				DeferredRenderer.EnableFXAA = !DeferredRenderer.EnableFXAA;
			}

			if (InputManager.WasKeyPressed(Input.Key.C))
			{
				var shadowQuality = (int)DeferredRenderer.ShadowQuality;
				shadowQuality = (shadowQuality + 1) % ((int)Graphics.Deferred.ShadowQuality.High + 1);
				DeferredRenderer.ShadowQuality = (Graphics.Deferred.ShadowQuality)shadowQuality;
			}

			if (InputManager.WasKeyPressed(Input.Key.V))
			{
				DeferredRenderer.EnableShadows = !DeferredRenderer.EnableShadows;
			}

			if (InputManager.WasKeyPressed(Input.Key.G))
			{
				if ((DebugFlags & Game.DebugFlags.GBuffer) == Game.DebugFlags.GBuffer)
					DebugFlags &= ~Game.DebugFlags.GBuffer;
				else
					DebugFlags |= Game.DebugFlags.GBuffer;
			}
		}

		protected override void RenderUI(float deltaTime)
		{
			base.RenderUI(deltaTime);

			var offsetY = 2;
			DebugFont.DrawText(DebugSprite, new Vector2(4, RequestedHeight - DebugFont.LineHeight * offsetY++), Vector4.One, "[f] FXAA: {0}", DeferredRenderer.EnableFXAA ? "Enabled" : "Disabled");
			DebugFont.DrawText(DebugSprite, new Vector2(4, RequestedHeight - DebugFont.LineHeight * offsetY++), Vector4.One, "[c] Shadow Quality: {0}", DeferredRenderer.ShadowQuality);
			DebugFont.DrawText(DebugSprite, new Vector2(4, RequestedHeight - DebugFont.LineHeight * offsetY++), Vector4.One, "[v] Shadows: {0}", DeferredRenderer.EnableShadows ? "Enabled" : "Disabled");
		}
	}
}
