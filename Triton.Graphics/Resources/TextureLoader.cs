using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;


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
			var filename = resource.Name + ".texture";
			bool srgb = parameters.Contains("srgb");

			using (var stream = FileSystem.OpenRead(filename))
			{
				var bitmap = (Bitmap)Bitmap.FromStream(stream);

				Renderer.PixelInternalFormat pif;
				Renderer.PixelFormat pf;
				Renderer.PixelType pt;

				switch (bitmap.PixelFormat)
				{
					case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
						pif = Renderer.PixelInternalFormat.Rgb8;
						pf = Renderer.PixelFormat.ColorIndex;
						pt = Renderer.PixelType.Bitmap;
						break;
					case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:
					case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
						pif = Renderer.PixelInternalFormat.Rgb5A1;
						pf = Renderer.PixelFormat.Bgr;
						pt = Renderer.PixelType.UnsignedShort5551Ext;
						break;
					case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
						pif = srgb ? Renderer.PixelInternalFormat.Srgb8 : Renderer.PixelInternalFormat.Rgb8;
						pf = Renderer.PixelFormat.Bgr;
						pt = Renderer.PixelType.UnsignedByte;
						break;
					case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
					case System.Drawing.Imaging.PixelFormat.Canonical:
					case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
						pif = srgb ? Renderer.PixelInternalFormat.SrgbAlpha : Renderer.PixelInternalFormat.Rgba;
						pf = Renderer.PixelFormat.Bgra;
						pt = Renderer.PixelType.UnsignedByte;
						break;
					default:
						throw new ArgumentException("ERROR: Unsupported Pixel Format " + bitmap.PixelFormat);
				}

				var data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				Renderer.RenderSystem.OnLoadedCallback onResourceLoaded = (handle, success, errors) =>
				{
					Common.Log.WriteLine(errors, success ? Common.LogLevel.Default : Common.LogLevel.Error);

					if (onLoaded != null)
						onLoaded(resource);
				};

				if (texture.Handle == -1)
					texture.Handle = Backend.RenderSystem.CreateTexture(bitmap.Width, bitmap.Height, data.Scan0, pf, pif, pt, onResourceLoaded);
				else
					Backend.RenderSystem.SetTextureData(texture.Handle, bitmap.Width, bitmap.Height, data.Scan0, pf, pif, pt, onResourceLoaded);

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
