using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Texture
	{
		public int Handle { get; internal set; }

		public int Width;
		public int Height;
		public Renderer.PixelInternalFormat PixelInternalFormat;
		public Renderer.PixelFormat PixelFormat;

		public Texture()
		{
			Handle = -1;
		}
	}
}
