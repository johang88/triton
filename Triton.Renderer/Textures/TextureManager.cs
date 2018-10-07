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
        private readonly TextureData[] _handles = new TextureData[MaxHandles];
        private short _nextFree = 0;
        private readonly int _defaultOpenGlHandle = 0;
        private bool _disposed = false;
        private readonly object _lock = new object();

        private readonly int[] _activeTextures;

        public TextureManager()
        {
            // Each empty handle will store the location of the next empty handle 
            for (var i = 0; i < _handles.Length; i++)
            {
                _handles[i].Id = (short)(i + 1);
            }

            _handles[_handles.Length - 1].Id = -1;

            // Create default texture
            GL.GenTextures(1, out _defaultOpenGlHandle);

            GL.BindTexture(OGL.TextureTarget.Texture2D, _defaultOpenGlHandle);
            GL.TexImage2D(OGL.TextureTarget.Texture2D, 0, OGL.PixelInternalFormat.Rgb8, 1, 1, 0, OGL.PixelFormat.Rgba, OGL.PixelType.Byte, new byte[] { 0, 255, 255 });
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            _activeTextures = new int[20];
            for (var i = 0; i < _activeTextures.Length; i++)
            {
                _activeTextures[i] = -1;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing || _disposed)
                return;

            for (var i = 0; i < _handles.Length; i++)
            {
                if (_handles[i].Initialized)
                    GL.DeleteTexture(_handles[i].OpenGLHandle);
                _handles[i].Initialized = false;
            }

            GL.DeleteTexture(_defaultOpenGlHandle);

            _disposed = true;
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
            if (_nextFree == -1)
            {
                return CreateHandle(-1, -1);
            }

            int index;
            lock (_lock)
            {
                index = _nextFree;
                _nextFree = _handles[_nextFree].Id;
            }

            var id = ++_handles[index].Id;
            _handles[index].Initialized = false;
            _handles[index].OpenGLHandle = 0;

            return CreateHandle(index, id);
        }

        public void Destroy(int handle)
        {
            ExtractHandle(handle, out int index, out int id);

            if (id == -1 || _handles[index].Id != id)
                return;

            lock (_lock)
            {
                _handles[index].Id = _nextFree;
                _nextFree = (short)index;
            }

            if (_handles[index].Initialized)
                GL.DeleteTexture(_handles[index].OpenGLHandle);

            _handles[index].Initialized = false;
        }

        public void SetPixelData(int handle, TextureTarget target, int width, int height, byte[] data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type, bool mipmap = true)
        {
            ExtractHandle(handle, out int index, out int id);

            if (id == -1 || _handles[index].Id != id)
                return;

            if (!_handles[index].Initialized)
                GL.GenTextures(1, out _handles[index].OpenGLHandle);

            _handles[index].Target = (OGL.TextureTarget)target;

            // Upload texture data to OpenGL
            GL.BindTexture((OGL.TextureTarget)(int)_handles[index].Target, _handles[index].OpenGLHandle);

            if (target == TextureTarget.TextureCubeMap)
            {
                for (var i = 0; i < 6; i++)
                {
                    GL.TexImage2D(OGL.TextureTarget.TextureCubeMapPositiveX + i, 0, (OGL.PixelInternalFormat)(int)internalFormat, width, height, 0, (OGL.PixelFormat)(int)format, (OGL.PixelType)(int)type, data);
                }
                if (mipmap)
                    GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
            }
            else
            {
                GL.TexImage2D((OGL.TextureTarget)(int)_handles[index].Target, 0, (OGL.PixelInternalFormat)(int)internalFormat, width, height, 0, (OGL.PixelFormat)(int)format, (OGL.PixelType)(int)type, data);
                if (mipmap)
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                GL.TexParameter((OGL.TextureTarget)(int)_handles[index].Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter((OGL.TextureTarget)(int)_handles[index].Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            }

            // The texture can now be used
            _handles[index].Initialized = true;
        }

        public void LoadDDS(int handle, byte[] data, out int width, out int height)
        {
            ExtractHandle(handle, out int index, out int id);

            width = height = 0;

            if (id == -1 || _handles[index].Id != id)
                return;

            if (_handles[index].Initialized)
                return; // TODO ?

            OGL.TextureTarget target;
            DDS.LoaderDDS.LoadFromStream(data, out _handles[index].OpenGLHandle, out target, out width, out height);

            _handles[index].Target = target;

            GL.Finish();

            _handles[index].Initialized = true;
        }

        public int GetActiveTexture(int textureUnit)
        {
            int index, id;
            ExtractHandle(_activeTextures[textureUnit], out index, out id);

            return _handles[index].OpenGLHandle;
        }

        public void Bind(int textureUnit, int handle)
        {
            if (_activeTextures[textureUnit] == handle)
                return;

            _activeTextures[textureUnit] = handle;

            OGL.TextureTarget target;
            var openGLHandle = GetOpenGLHande(handle, out target);

            GLWrapper.BindMultiTexture(TextureUnit.Texture0 + textureUnit, target, openGLHandle);
        }

        public int GetOpenGLHande(int handle)
        {
            ExtractHandle(handle, out int index, out int id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
                return _defaultOpenGlHandle;

            return _handles[index].OpenGLHandle;
        }

        public int GetOpenGLHande(int handle, out OGL.TextureTarget target)
        {
            ExtractHandle(handle, out int index, out int id);

            target = OGL.TextureTarget.Texture2D;

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
                return _defaultOpenGlHandle;

            target = _handles[index].Target;
            return _handles[index].OpenGLHandle;
        }

        struct TextureData
        {
            public int OpenGLHandle;
            public short Id;
            public bool Initialized;
            public OGL.TextureTarget Target;
        }
    }
}
