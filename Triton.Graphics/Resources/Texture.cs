using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Texture : IDisposable
	{
        private Backend _backend;

        /// <summary>
        /// Texture is assumed to own the handle
        /// </summary>
        public int Handle { get; internal set; }

		public int Width { get; internal set; }
		public int Height { get; internal set; }
        public Renderer.PixelInternalFormat PixelInternalFormat { get; internal set; }
        public Renderer.PixelFormat PixelFormat { get; internal set; }

        public Texture(Backend backend)
		{
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));

            Handle = -1;
        }

        public void Dispose()
        {
            _backend.RenderSystem.DestroyTexture(Handle);
            Handle = -1;
        }
	}
}
