using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Texture : Triton.Common.Resource
	{
		public int Handle { get; internal set; }

		public int Width;
		public int Height;
		public Renderer.PixelInternalFormat PixelInternalFormat;
		public Renderer.PixelFormat PixelFormat;

		public Texture(string name, string parameters)
			: base(name, parameters)
		{
			Handle = -1;
		}
	}
}
