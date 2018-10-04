using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Triton.Graphics.Resources
{
	class TextureLoader : Triton.Common.IResourceLoader<Texture>
	{
		private readonly Backend Backend;
		private readonly Triton.Common.IO.FileSystem FileSystem;

        public bool SupportsStreaming => false;

        public TextureLoader(Backend backend, Triton.Common.IO.FileSystem fileSystem)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			Backend = backend;
			FileSystem = fileSystem;
		}

		public string Extension { get { return ".dds"; } }
		public string DefaultFilename { get { return "/textures/missing_n.dds"; } }

		public object Create(string name, string parameters)
		{
			return new Texture(name, parameters);
		}

		public void Load(object resource, byte[] data)
		{
			var texture = (Texture)resource;
			var filename = texture.Name + ".dds";
			if (!FileSystem.FileExists(filename))
			{
				Common.Log.WriteLine(string.Format("Missing texture {0}", filename), Common.LogLevel.Error);

				if (texture.Name.EndsWith("_n"))
					filename = "/textures/missing_n.dds";
				else
					filename = "/textures/missing.dds";
			}

            texture.Handle = Backend.RenderSystem.CreateFromDDS(data, out texture.Width, out texture.Height);
        }

		public void Unload(object resource)
		{
			var texture = (Texture)resource;
			Backend.RenderSystem.DestroyTexture(texture.Handle);
			texture.Handle = -1;
		}
	}
}
