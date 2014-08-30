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

		public TextureLoader(Backend backend, Triton.Common.IO.FileSystem fileSystem)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			Backend = backend;
			FileSystem = fileSystem;
		}

		public Common.Resource Create(string name, string parameters)
		{
			return new Texture(name, parameters);
		}

		public void Load(Common.Resource resource, string parameters, Action<Common.Resource> onLoaded)
		{
			var texture = (Texture)resource;
			var filename = resource.Name + ".dds";
			bool srgb = parameters.Contains("srgb");

			if (!FileSystem.FileExists(filename))
			{
				Common.Log.WriteLine(string.Format("Missing texture {0}", filename), Common.LogLevel.Error);

				if (resource.Name.EndsWith("_n"))
					filename = "/textures/missing_n.dds";
				else
					filename = "/textures/missing.dds";
			}

			using (var stream = FileSystem.OpenRead(filename))
			using (var data = new System.IO.MemoryStream(1024 * 1024))
			{
				var count =0 ;
				var buffer = new byte[4096];
				while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
				{
					data.Write(buffer, 0, count);
				}

				Renderer.RenderSystem.OnLoadedCallback onResourceLoaded = (handle, success, errors) =>
				{
					Common.Log.WriteLine(errors, success ? Common.LogLevel.Default : Common.LogLevel.Error);

					if (onLoaded != null)
						onLoaded(resource);
				};

				texture.Handle = Backend.RenderSystem.CreateFromDDS(data.GetBuffer(), onResourceLoaded);
				resource.Parameters = parameters;
			}
		}

		public void Unload(Common.Resource resource)
		{
			var texture = (Texture)resource;
			Backend.RenderSystem.DestroyTexture(texture.Handle);
			texture.Handle = -1;
		}
	}
}
