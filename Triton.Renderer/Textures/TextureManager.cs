using OpenTK.Graphics.OpenGL;
using OGL = OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer.Textures
{
	class TextureManager : IDisposable
	{
		const int MaxHandles = 4096;
		private readonly TextureData[] Handles = new TextureData[MaxHandles];
		private short NextFree = 0;
		private readonly int DefaultOpenGLHandle = 0; // TODO
		private bool Disposed = false;
		private readonly object Lock = new object();

		public TextureManager()
		{
			// Each empty handle will store the location of the next empty handle 
			for (var i = 0; i < Handles.Length; i++)
			{
				Handles[i].Id = (short)(i + 1);
			}

			Handles[Handles.Length - 1].Id = -1;

			// Create default texture
			GL.GenTextures(1, out DefaultOpenGLHandle);

			GL.BindTexture(TextureTarget.Texture2D, DefaultOpenGLHandle);
			GL.TexImage2D(TextureTarget.Texture2D, 0, OGL.PixelInternalFormat.Rgb8, 1, 1, 0, OGL.PixelFormat.Rgba, OGL.PixelType.Byte, new byte[] { 0, 255, 255 });
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			GL.Finish();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (!isDisposing || Disposed)
				return;

			for (var i = 0; i < Handles.Length; i++)
			{
				if (Handles[i].Initialized)
					GL.DeleteTexture(Handles[i].OpenGLHandle);
				Handles[i].Initialized = false;
			}

			GL.DeleteTexture(DefaultOpenGLHandle);

			Disposed = true;
		}

		int CreateHandle(int index, int id)
		{
			return (index << 16) | id;
		}

		void ExtractHandle(int handle, out int index, out int id)
		{
			id = handle & 0x0000FFFF;
			index = handle >> 16;
		}

		public int Create()
		{
			if (NextFree == -1)
			{
				return CreateHandle(-1, -1);
			}

			int index;
			lock (Lock)
			{
				index = NextFree;
				NextFree = Handles[NextFree].Id;
			}

			var id = ++Handles[index].Id;
			Handles[index].Initialized = false;
			Handles[index].OpenGLHandle = 0;

			return CreateHandle(index, id);
		}

		public void Destroy(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			lock (Lock)
			{
				if (NextFree == -1)
				{
					Handles[index].Id = -1;
					NextFree = (short)index;
				}
				else
				{
					Handles[index].Id = NextFree;
					NextFree = (short)index;
				}
			}

			if (Handles[index].Initialized)
				GL.DeleteTexture(Handles[index].OpenGLHandle);

			Handles[index].Initialized = false;
		}

		public void SetPixelData(int handle, int width, int height, byte[] data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			if (!Handles[index].Initialized)
				GL.GenTextures(1, out Handles[index].OpenGLHandle);

			// Upload texture data to OpenGL
			GL.BindTexture(TextureTarget.Texture2D, Handles[index].OpenGLHandle);
			GL.TexImage2D(TextureTarget.Texture2D, 0, (OGL.PixelInternalFormat)(int)internalFormat, width, height, 0, (OGL.PixelFormat)(int)format, (OGL.PixelType)(int)type, data);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.Finish();

			// The texture can now be used
			Handles[index].Initialized = true;
		}

		public void SetPixelData(int handle, int width, int height, IntPtr data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			if (!Handles[index].Initialized)
				GL.GenTextures(1, out Handles[index].OpenGLHandle);

			// Upload texture data to OpenGL
			GL.BindTexture(TextureTarget.Texture2D, Handles[index].OpenGLHandle);
			GL.TexImage2D(TextureTarget.Texture2D, 0, (OGL.PixelInternalFormat)(int)internalFormat, width, height, 0, (OGL.PixelFormat)(int)format, (OGL.PixelType)(int)type, data);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.Finish();

			// The texture can now be used
			Handles[index].Initialized = true;
		}

		public int GetOpenGLHande(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
				return DefaultOpenGLHandle;

			return Handles[index].OpenGLHandle;
		}

		struct TextureData
		{
			public int OpenGLHandle;
			public short Id;
			public bool Initialized;
		}
	}
}
