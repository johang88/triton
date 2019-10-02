using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Triton.Graphics.Resources
{
	class TextureSerializer : Triton.Resources.IResourceSerializer<Texture>
	{
		private readonly Backend _backend;
		private readonly Triton.IO.FileSystem _fileSystem;

        public bool SupportsStreaming => false;

        public TextureSerializer(Backend backend, Triton.IO.FileSystem fileSystem)
		{
            _backend = backend ?? throw new ArgumentNullException("backend");
			_fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
		}

		public string Extension { get { return ".dds"; } }
		public string DefaultFilename { get { return "/textures/missing_n.dds"; } }

        public object Create(Type type)
            => new Texture(_backend);

		public Task Deserialize(object resource, byte[] data)
		{
            var texture = (Texture)resource;

            texture.Handle = _backend.RenderSystem.CreateFromDDS(data, out var width, out var height);
            texture.Width = width;
            texture.Height = height;

            return Task.FromResult(0);
        }

        public byte[] Serialize(object resource)
            => throw new NotImplementedException();
    }
}
