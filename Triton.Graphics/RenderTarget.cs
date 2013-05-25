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
		internal readonly Resources.Texture[] Textures;

		public RenderTarget(int handle, int width, int height, Resources.Texture[] textures)
		{
			Handle = handle;
			Width = width;
			Height = height;
			Textures = textures;
		}

		public Resources.Texture GetTexture(int i)
		{
			return Textures[i];
		}
	}
}
