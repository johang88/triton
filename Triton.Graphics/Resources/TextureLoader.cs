using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Triton.Common;

namespace Triton.Graphics.Resources
{
	class TextureLoader : Triton.Common.IResourceLoader<Texture>
	{
		private readonly Backend _backend;
		private readonly Triton.Common.IO.FileSystem _fileSystem;
        private readonly ResourceManager _resourceManager;

        public bool SupportsStreaming => false;

        public TextureLoader(Backend backend, Triton.Common.IO.FileSystem fileSystem)
		{
            _backend = backend ?? throw new ArgumentNullException("backend");
			_fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
		}

		public string Extension { get { return ".dds"; } }
		public string DefaultFilename { get { return "/textures/missing_n.dds"; } }

        public object Create(Type type)
            => new Texture();

		public Task Load(object resource, byte[] data)
		{
            var texture = (Texture)resource;
            texture.Handle = _backend.RenderSystem.CreateFromDDS(data, out texture.Width, out texture.Height);

            return Task.FromResult(0);
        }

		public void Unload(object resource)
		{
			var texture = (Texture)resource;
			_backend.RenderSystem.DestroyTexture(texture.Handle);
			texture.Handle = -1;
		}
	}
}
