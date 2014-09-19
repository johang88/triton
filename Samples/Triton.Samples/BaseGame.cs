﻿using System;
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
			ResolutionScale = 1.0f;
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

			if (InputManager.WasKeyPressed(Input.Key.C))
			{
				var shadowQuality = (int)DeferredRenderer.Settings.ShadowQuality;
				shadowQuality = (shadowQuality + 1) % ((int)Graphics.Deferred.ShadowQuality.High + 1);
				DeferredRenderer.Settings.ShadowQuality = (Graphics.Deferred.ShadowQuality)shadowQuality;
			}

			if (InputManager.WasKeyPressed(Input.Key.V))
			{
				DeferredRenderer.Settings.EnableShadows = !DeferredRenderer.Settings.EnableShadows;
			}

			if (InputManager.WasKeyPressed(Input.Key.B))
			{
				PostEffectManager.HDRSettings.EnableBloom = !PostEffectManager.HDRSettings.EnableBloom;
			}

			if (InputManager.WasKeyPressed(Input.Key.L))
			{
				PostEffectManager.HDRSettings.EnableLensFlares = !PostEffectManager.HDRSettings.EnableLensFlares;
			}

			if (InputManager.WasKeyPressed(Input.Key.P))
			{
				if ((DebugFlags & Game.DebugFlags.Physics) == Game.DebugFlags.Physics)
					DebugFlags &= ~Game.DebugFlags.Physics;
				else
					DebugFlags |= Game.DebugFlags.Physics;
			}

			if (InputManager.WasKeyPressed(Input.Key.G))
			{
				if ((DebugFlags & Game.DebugFlags.GBuffer) == Game.DebugFlags.GBuffer)
					DebugFlags &= ~Game.DebugFlags.GBuffer;
				else
					DebugFlags |= Game.DebugFlags.GBuffer;
			}

			if (InputManager.WasKeyPressed(Input.Key.F))
			{
				var aa = (int)PostEffectManager.AntiAliasing;
				var lastAa = (int)Graphics.Post.AntiAliasing.Last;
				PostEffectManager.AntiAliasing = (Graphics.Post.AntiAliasing)((aa + 1) % lastAa);
			}
		}

		protected override void RenderUI(float deltaTime)
		{
			base.RenderUI(deltaTime);

			var offsetY = 2;
			DebugFont.DrawText(SpriteRenderer, new Vector2(4, RequestedHeight - DebugFont.LineHeight * offsetY++), Vector4.One, "[c] Shadow Quality: {0}", DeferredRenderer.Settings.ShadowQuality);
			DebugFont.DrawText(SpriteRenderer, new Vector2(4, RequestedHeight - DebugFont.LineHeight * offsetY++), Vector4.One, "[v] Shadows: {0}", DeferredRenderer.Settings.EnableShadows ? "Enabled" : "Disabled");
			DebugFont.DrawText(SpriteRenderer, new Vector2(4, RequestedHeight - DebugFont.LineHeight * offsetY++), Vector4.One, "[f] Anti Aliasing: {0}", PostEffectManager.AntiAliasing);
			DebugFont.DrawText(SpriteRenderer, new Vector2(4, RequestedHeight - DebugFont.LineHeight * offsetY++), Vector4.One, "[b] Bloom: {0}", PostEffectManager.HDRSettings.EnableBloom ? "Enabled" : "Disabled");
			DebugFont.DrawText(SpriteRenderer, new Vector2(4, RequestedHeight - DebugFont.LineHeight * offsetY++), Vector4.One, "[l] Lens Flares: {0}", PostEffectManager.HDRSettings.EnableLensFlares ? "Enabled" : "Disabled");
			DebugFont.DrawText(SpriteRenderer, new Vector2(4, RequestedHeight - DebugFont.LineHeight * offsetY++), Vector4.One, "[f] Camera Position: {0}", Common.StringConverter.ToString(Camera.Position));
		}
	}
}
