using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	/// <summary>
	/// A render target is used to render to offscreen textures.
	/// It contains references to one or more textures that can be used for rendering.
	/// </summary>
	public class RenderTarget
	{
		internal int Handle;
		internal int Width;
		internal int Height;
		public Resources.Texture[] Textures { get; internal set; }

		public bool IsReady { get; internal set; }

		public RenderTarget(int width, int height)
		{
			Handle = -1;
			Width = width;
			Height = height;
		}

		public Resources.Texture GetTexture(int i)
		{
			return Textures[i];
		}
	}
}
