using OpenTK.Graphics.OpenGL;
using OGL = OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer.Meshes
{
    public class MeshManager : IDisposable
    {
        const int MaxHandles = 4096;
        private readonly MeshData[] _handles = new MeshData[MaxHandles];
        private int _nextFree = 0;
        private bool _disposed = false;
        private readonly object _lock = new object();
        private readonly BufferManager _bufferManager;
        private int _activeMeshHandle = 0;

        public MeshManager(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

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
                    GL.DeleteVertexArrays(1, ref _handles[i].VertexArrayObjectID);
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

        public int Create()
        {
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
            _handles[index].VertexBufferID = null;
            _handles[index].IndexBufferID = -1;
            _handles[index].Type = IndexType.UnsignedInt;


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
                GL.DeleteVertexArrays(1, ref _handles[index].VertexArrayObjectID);
            }

            _handles[index].Initialized = false;
        }

        public void Initialize(int handle, int triangleCount, int vertexBufferId, int indexBufferId, IndexType indexType = IndexType.UnsignedInt)
        {
            Initialize(handle, triangleCount, new int[] { vertexBufferId }, indexBufferId, indexType);
        }

        public void Initialize(int handle, int triangleCount, int[] vertexBufferId, int indexBufferId, IndexType indexType = IndexType.UnsignedInt)
        {
            if (vertexBufferId.Length == 0)
                throw new ArgumentException("missing vertex buffer");

            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id)
                return;

            if (_handles[index].Initialized)
                return;

            _handles[index].VertexBufferID = vertexBufferId;
            _handles[index].IndexBufferID = indexBufferId;
            _handles[index].TriangleCount = triangleCount;
            _handles[index].Type = indexType;

            GL.GenVertexArrays(1, out _handles[index].VertexArrayObjectID);
            GL.BindVertexArray(_handles[index].VertexArrayObjectID);

            for (var i = 0; i < vertexBufferId.Length; i++)
            {
                var bufferHandle = _bufferManager.GetOpenGLHandle(_handles[index].VertexBufferID[i]);
                var vertexFormat = _bufferManager.GetVertexFormat(vertexBufferId[i]);

                if (GLWrapper.ExtDirectStateAccess)
                {
                    GL.Ext.VertexArrayBindVertexBuffer(_handles[index].VertexArrayObjectID, i, bufferHandle, IntPtr.Zero, vertexFormat.Size);
                    SetVertexFormat(_handles[index].VertexArrayObjectID, i, vertexFormat);
                }
                else
                {
                    _bufferManager.Bind(_handles[index].VertexBufferID[i]);
                    SetVertexFormat(vertexFormat);
                }
            }

            _bufferManager.Bind(_handles[index].IndexBufferID);
            GL.BindVertexArray(_activeMeshHandle);

            _handles[index].Initialized = true;
        }

        public void SetTriangleCount(int handle, int triangleCount)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id)
                return;

            _handles[index].TriangleCount = triangleCount;
        }

        public void SetIndexBuffer(int handle, int indexBufferId, int triangleCount)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id)
                return;

            _handles[index].TriangleCount = triangleCount;

            GL.BindVertexArray(_handles[index].VertexArrayObjectID);
            _handles[index].IndexBufferID = indexBufferId;

            _bufferManager.Bind(_handles[index].IndexBufferID);

            ExtractHandle(_activeMeshHandle, out index, out id);
            GL.BindVertexArray(_handles[index].VertexArrayObjectID);
        }

        private void SetVertexFormat(int vao, int bindingIndex, VertexFormat vertexFormat)
        {
            for (var v = 0; v < vertexFormat.Elements.Length; v++)
            {
                var element = vertexFormat.Elements[v];
                var index = (int)element.Semantic;

                GL.Ext.EnableVertexArrayAttrib(vao, index);
                GL.Ext.VertexArrayVertexAttribBinding(vao, index, bindingIndex);

                GL.Ext.VertexArrayVertexAttribFormat(vao, index, element.Count, (ExtDirectStateAccess)element.Type, element.Normalized, element.Offset);
                GL.Ext.VertexArrayVertexBindingDivisor(vao, bindingIndex, element.Divisor);
            }
        }

        private void SetVertexFormat(VertexFormat vertexFormat)
        {
            for (var i = 0; i < vertexFormat.Elements.Length; i++)
            {
                var element = vertexFormat.Elements[i];
                var index = (int)element.Semantic;
                GL.EnableVertexAttribArray(index);
                GL.VertexAttribPointer(index, element.Count, (VertexAttribPointerType)(int)element.Type, element.Normalized, vertexFormat.Size, element.Offset);
                GL.VertexAttribDivisor(index, element.Divisor);
            }
        }

        public void Render(int handle, PrimitiveType primitiveType)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
            {
                return;
            }

            if (_activeMeshHandle != handle)
            {
                GL.BindVertexArray(_handles[index].VertexArrayObjectID);
                _activeMeshHandle = handle;
            }

            GL.DrawElements(primitiveType, _handles[index].TriangleCount * 3, (DrawElementsType)_handles[index].Type, IntPtr.Zero);
        }

        public void Render(int handle, PrimitiveType primitiveType, int offset, int count)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
            {
                return;
            }

            if (_activeMeshHandle != handle)
            {
                GL.BindVertexArray(_handles[index].VertexArrayObjectID);
                _activeMeshHandle = handle;
            }

            GL.DrawElements(primitiveType, count, (DrawElementsType)_handles[index].Type, offset);
        }

        public void RenderInstanced(int handle, int count, int instanceBufferId)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
            {
                return;
            }

            var vao = _handles[index].VertexArrayObjectID;

            if (_activeMeshHandle != handle)
            {
                GL.BindVertexArray(vao);
                _activeMeshHandle = handle;
            }

            // Setup the instancing buffer data
            var instanceBufferHandle = _bufferManager.GetOpenGLHandle(instanceBufferId);
            var vertexFormat = _bufferManager.GetVertexFormat(instanceBufferId);
            var instanceBufferIndex = _handles[index].VertexBufferID.Length;

            if (GLWrapper.ExtDirectStateAccess)
            {
                GL.Ext.VertexArrayBindVertexBuffer(_handles[index].VertexArrayObjectID, instanceBufferIndex, instanceBufferHandle, IntPtr.Zero, vertexFormat.Size);
                SetVertexFormat(vao, instanceBufferIndex, vertexFormat);
            }
            else
            {
                _bufferManager.Bind(instanceBufferId);
                SetVertexFormat(vertexFormat);
            }

            // And draw all the stuff
            GL.DrawElementsInstanced(PrimitiveType.Triangles, _handles[index].TriangleCount * 3, (DrawElementsType)_handles[index].Type, IntPtr.Zero, count);
        }

        public void GetRenderData(int handle, out int triangleCount, out int vertexArrayObjectId)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
            {
                triangleCount = vertexArrayObjectId = -1;
                return;
            }

            triangleCount = _handles[index].TriangleCount;
            vertexArrayObjectId = _handles[index].VertexArrayObjectID;
        }

        public bool GetMeshData(int handle, out int vertexBufferId, out int indexBufferId)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
            {
                vertexBufferId = indexBufferId = -1;
                return false;
            }

            vertexBufferId = _handles[index].VertexBufferID[0];
            indexBufferId = _handles[index].IndexBufferID;

            return true;
        }

        struct MeshData
        {
            public bool Initialized;
            public short Id;

            public int VertexArrayObjectID;
            public int[] VertexBufferID;
            public int IndexBufferID;
            public int TriangleCount;
            public IndexType Type;
        }
    }
}
