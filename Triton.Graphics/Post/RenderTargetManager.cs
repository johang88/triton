using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer.RenderTargets;
using Triton.Utility;

namespace Triton.Graphics.Post
{
	/// <summary>
	/// Manages temporary render targets, targets can be released and reused.
	/// </summary>
	public class RenderTargetManager
	{
		private static int Counter = 0;
		private readonly Backend Backend;
		private readonly List<RenderTargetWrapper> TemporaryRenderTargets = new List<RenderTargetWrapper>();

		public RenderTargetManager(Backend backend)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");

			Backend = backend;
		}

		public RenderTargetWrapper Allocate(int width, int height, Renderer.PixelInternalFormat pixelInternalFormat)
		{
			for (var i = 0; i < TemporaryRenderTargets.Count; i++)
			{
				var target = TemporaryRenderTargets[i];
				if (target.Width == width && target.Height == height && target.PixelInternalFormat == pixelInternalFormat)
				{
					return target;
				}
			}

			return new RenderTargetWrapper(
				width, height, pixelInternalFormat,
				this,
				Backend.CreateRenderTarget("tmp_" + StringConverter.ToString(Counter++), new Definition(width, height, false, new List<Definition.Attachment>()
				{
					new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0)
				}))
			);
		}

		public void Release(RenderTargetWrapper renderTarget)
		{
			TemporaryRenderTargets.Add(renderTarget);
		}

		public class RenderTargetWrapper : IDisposable
		{
			private readonly RenderTargetManager Manager;
			internal int Width;
			internal int Height;
			internal Renderer.PixelInternalFormat PixelInternalFormat;

			public readonly RenderTarget RenderTarget;

			public RenderTargetWrapper(int width, int height, Renderer.PixelInternalFormat pixelInternalFormat, RenderTargetManager manager, RenderTarget renderTarget)
			{
				Width = width;
				Height = height;
				PixelInternalFormat = pixelInternalFormat;

				Manager = manager;
				RenderTarget = renderTarget;
			}

			public void Dispose()
			{
				Manager.Release(this);
			}
		}
	}
}
