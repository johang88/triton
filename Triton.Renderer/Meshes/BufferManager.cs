using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OGL = OpenTK.Graphics.OpenGL;

namespace Triton.Renderer.Meshes
{
    public class BufferManager : IDisposable
    {
        const int SharedBufferSize = 128 * 1024 * 1024;
        const int MaxHandles = 8192;

        private readonly BufferData[] _handles = new BufferData[MaxHandles];
        private int _nextFree = 0;
        private bool _disposed = false;
        private readonly object _lock = new object();

        private List<SharedBufferInfo> _sharedBuffers = new List<SharedBufferInfo>();

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
                if (_handles[i].Initialized && !_handles[i].Shared)
                {
                    GL.DeleteBuffers(1, new int[] { _handles[i].BufferID });
                }
                _handles[i].Initialized = false;
            }

            foreach (var sharedBuffer in _sharedBuffers)
            {
                GL.DeleteBuffers(1, new int[] { sharedBuffer.BufferId });
            }

            _disposed = true;
        }

        int CreateHandle(int index, int id)
        {
            return (index << 16) | id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            if (data == IntPtr.Zero)
                return;

            ExtractHandle(handle, out var index, out var id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
                return;

            if (!_handles[index].Mutable)
                throw new InvalidOperationException();

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

            if (!_handles[index].Initialized && _handles[index].Mutable)
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
                if (_handles[index].Target != OGL.BufferTarget.ArrayBuffer && _handles[index].Target != OGL.BufferTarget.ElementArrayBuffer)
                    throw new InvalidOperationException();

                // Find shared buffer
                var vertexFormatDescription = _handles[index].VertexFormat?.ToString();
                SharedBufferInfo sharedBuffer = null;
                foreach (var buffer in _sharedBuffers)
                {
                    if (buffer.Target == _handles[index].Target)
                    {
                        if (buffer.Target == OGL.BufferTarget.ArrayBuffer && buffer.VertexFormatDescription != vertexFormatDescription)
                            continue;

                        sharedBuffer = buffer;
                    }
                }

                // Allocate new shared buffer in none exists
                if (sharedBuffer == null)
                {
                    GL.CreateBuffers(1, out int bufferId);

                    sharedBuffer = new SharedBufferInfo
                    {
                        CurrentSize = 0,
                        MaxSize = SharedBufferSize,
                        Target = _handles[index].Target,
                        VertexFormatDescription = _handles[index].VertexFormat?.ToString(),
                        VertexFormat = _handles[index].VertexFormat,
                        BufferId = bufferId
                    };

                    _sharedBuffers.Add(sharedBuffer);

                    // Upload initial data
                    CheckError();
                    GL.NamedBufferData(sharedBuffer.BufferId, sharedBuffer.MaxSize, IntPtr.Zero, BufferUsageHint.StaticDraw);
                    CheckError();
                }

                _handles[index].BufferID = sharedBuffer.BufferId;

                // TODO: Memory management with blocks and stuff!

                // Calculate offsets and upload data
                var offset = sharedBuffer.CurrentSize;

                if (offset + (int)dataLength > sharedBuffer.MaxSize)
                    throw new InvalidOperationException("shared buffer to small");

                sharedBuffer.CurrentSize += (int)dataLength;
                _handles[index].Shared = true;
                _handles[index].Offset = offset;

                GL.NamedBufferSubData(sharedBuffer.BufferId, (IntPtr)offset, dataLength, data);
                CheckError();
            }

            _handles[index].Initialized = true;
        }

        void CheckError()
        {
            var error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                throw new InvalidOperationException();
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOpenGLHandle(int handle)
        {
            ExtractHandle(handle, out var index, out var id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
                return -1;

            return _handles[index].BufferID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VertexFormat GetVertexFormat(int handle)
        {
            ExtractHandle(handle, out var index, out var id);

            if (id == -1 || _handles[index].Id != id)
                return null;

            return _handles[index].VertexFormat;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOffset(int handle)
        {
            ExtractHandle(handle, out var index, out var id);

            if (id == -1 || _handles[index].Id != id)
                return 0;

            return _handles[index].Offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(int handle)
        {
            ExtractHandle(handle, out var index, out var id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
                return;

            GL.BindBuffer(_handles[index].Target, _handles[index].BufferID);
        }

        struct BufferData
        {
            public bool Initialized;
            public short Id;

            public int BufferID;
            public OGL.BufferTarget Target;
            public VertexFormat VertexFormat;
            public bool Mutable;
            public int Offset;
            public int Size;
            public bool Shared;
        }

        class SharedBufferInfo
        {
            public OGL.BufferTarget Target;
            public string VertexFormatDescription;
            public VertexFormat VertexFormat;
            public int BufferId;
            public int MaxSize;
            public int CurrentSize;
        }
    }
}
