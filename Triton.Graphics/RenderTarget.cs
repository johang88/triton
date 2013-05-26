using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public class RenderTarget
	{
		internal int Handle;
		internal readonly int Width;
		internal readonly int Height;
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
