using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OGL = OpenTK.Graphics.OpenGL;

namespace Triton.Renderer.Meshes
{
    public class BufferManager : IDisposable
    {
        const int MaxHandles = 8192;
        private readonly BufferData[] _handles = new BufferData[MaxHandles];
        private int _nextFree = 0;
        private bool _disposed = false;
        private readonly object _lock = new object();

        public BufferManager()
        {
            // Each empty handle will store the location of the next empty handle 
            for (var i = 0; i < _handles.Length; i++)
            {
                _handles[i].Id = (short)(i + 1);
            }

            _handles[_handles.Length - 1].Id = -1;
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
                {
                    GL.DeleteBuffers(1, new int[] { _handles[i].BufferID });
                }
                _handles[i].Initialized = false;
            }

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

        public int Create(BufferTarget target, bool mutable, VertexFormat vertexFormat = null)
        {
            if (target == BufferTarget.ArrayBuffer && vertexFormat == null)
                throw new Exception("missing vertex format");

            if (_nextFree == -1)
            {
                CreateHandle(-1, -1);
            }

            int index;
            lock (_lock)
            {
                index = _nextFree;
                _nextFree = _handles[_nextFree].Id;
            }

            var id = ++_handles[index].Id;
            _handles[index].Initialized = false;
            _handles[index].Target = (OGL.BufferTarget)(int)target;
            _handles[index].VertexFormat = vertexFormat;
            _handles[index].Mutable = mutable;

            return CreateHandle(index, id);
        }

        public void Destroy(int handle)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id)
                return;

            lock (_lock)
            {
                _handles[index].Id = (short)_nextFree;
                _nextFree = index;
            }

            if (_handles[index].Initialized)
            {
                GL.DeleteBuffers(1, new int[] { _handles[index].BufferID });
            }

            _handles[index].Initialized = false;
        }

        public void SetDataDirect(int handle, IntPtr dataLength, IntPtr data, bool stream)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
                return;

            if (data == IntPtr.Zero)
                return;

            GL.InvalidateBufferData(_handles[index].BufferID);
            GLWrapper.NamedBufferData(_handles[index].Target, _handles[index].BufferID, dataLength, data, stream ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw);

            _handles[index].Size = (int)dataLength;
        }

        public void SetData<T>(int handle, T[] data, bool stream)
            where T : struct
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id)
                return;

            if (data == null)
                return;

            if (!_handles[index].Initialized)
            {
                GL.GenBuffers(1, out _handles[index].BufferID);
            }

            var dataLength = new IntPtr(data.Length * Marshal.SizeOf(typeof(T)));
            _handles[index].Size = (int)dataLength;

            if (_handles[index].Mutable)
            {
                GLWrapper.NamedBufferData(_handles[index].Target, _handles[index].BufferID, dataLength, data, stream ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw);
            }
            else
            {
                GLWrapper.NamedBufferStorage(_handles[index].Target, _handles[index].BufferID, (IntPtr)dataLength, data, 0);
            }

            _handles[index].Initialized = true;
        }

        public void GetOpenGLHandle(int handle, out int glHandle, out OGL.BufferTarget target)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
            {
                glHandle = -1;
                target = OGL.BufferTarget.ArrayBuffer;
                return;
            }

            glHandle = _handles[index].BufferID;
            target = _handles[index].Target;
        }

        public int GetOpenGLHandle(int handle)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
                return -1;

            return _handles[index].BufferID;
        }

        public VertexFormat GetVertexFormat(int handle)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id)
                return null;

            return _handles[index].VertexFormat;
        }

        public void Bind(int handle)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
                return;

            GL.BindBuffer(_handles[index].Target, _handles[index].BufferID);
        }

        public void Unbind(int handle)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
                return;

            GL.BindBuffer(_handles[index].Target, 0);
        }

        struct BufferData
        {
            public bool Initialized;
            public short Id;

            public int BufferID;
            public OGL.BufferTarget Target;
            public VertexFormat VertexFormat;
            public bool Mutable;
            public int Size;
        }
    }
}
