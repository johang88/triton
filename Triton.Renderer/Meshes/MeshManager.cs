using OpenTK.Graphics.OpenGL;
using OGL = OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Triton.Renderer.Meshes
{
    public class MeshManager : IDisposable
    {
        const int MaxVertexArrayObjects = 128;
        const int MaxHandles = 4096;
        private readonly MeshData[] _handles = new MeshData[MaxHandles];
        private int _nextFree = 0;
        private bool _disposed = false;
        private readonly object _lock = new object();
        private readonly BufferManager _bufferManager;
        private int _activeMeshHandle = 0;
        private VertexArrayObjects[] _vertexArrayObjects = new VertexArrayObjects[MaxVertexArrayObjects];
        private int _vertexArrayObjectCount = 0;
        private DrawElementsIndirectCommand[] _drawIndirectCommands = new DrawElementsIndirectCommand[4096];

        private int[] _indirectBufferHandles = new int[16];
        private int _currentIndirectBuffer = 0;

        public MeshManager(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

            // Each empty handle will store the location of the next empty handle 
            for (var i = 0; i < _handles.Length; i++)
            {
                _handles[i].Id = (short)(i + 1);
            }

            _handles[_handles.Length - 1].Id = -1;


            GL.CreateBuffers(_indirectBufferHandles.Length, _indirectBufferHandles);
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

            for (var i = 0; i < _vertexArrayObjects.Length; i++)
            {
                GL.DeleteVertexArrays(1, ref _vertexArrayObjects[i].VertexArrayObjectId);
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
            _handles[index].VertexBufferId = -1;
            _handles[index].IndexBufferId = -1;
            _handles[index].Type = IndexType.UnsignedInt;

            return CreateHandle(index, id);
        }

        public void Destroy(int handle)
        {
            ExtractHandle(handle, out var index, out var id);

            if (id == -1 || _handles[index].Id != id)
                return;

            lock (_lock)
            {
                _handles[index].Id = (short)_nextFree;
                _nextFree = index;
            }

            _handles[index].Initialized = false;
        }

        public void Initialize(int handle, int triangleCount, int vertexBufferId, int indexBufferId, IndexType indexType = IndexType.UnsignedInt)
        {
            ExtractHandle(handle, out var index, out var id);

            if (id == -1 || _handles[index].Id != id)
                return;

            if (_handles[index].Initialized)
                return;

            _handles[index].VertexBufferId = vertexBufferId;
            _handles[index].IndexBufferId = indexBufferId;
            _handles[index].PrimitiveCount = triangleCount * 3;
            _handles[index].Type = indexType;

            var vertexBufferGLHandle = _bufferManager.GetOpenGLHandle(_handles[index].VertexBufferId);
            var indexBufferGLHandle = _bufferManager.GetOpenGLHandle(_handles[index].IndexBufferId);

            // Find vertex array object
            var vaoIndex = -1;
            for (var i = 0; i < _vertexArrayObjectCount; i++)
            {
                if (_vertexArrayObjects[i].VertexBufferGLHandle == vertexBufferGLHandle && _vertexArrayObjects[i].IndexBufferGLHandle == indexBufferGLHandle)
                {
                    vaoIndex = i;
                }
            }

            // Allocate new if neeed
            if (vaoIndex == -1)
            {
                vaoIndex = _vertexArrayObjectCount++;
                if (vaoIndex >= MaxVertexArrayObjects) throw new InvalidOperationException("out of vertex array objects");

                _vertexArrayObjects[vaoIndex].VertexBufferGLHandle = vertexBufferGLHandle;
                _vertexArrayObjects[vaoIndex].IndexBufferGLHandle = indexBufferGLHandle;
                _vertexArrayObjects[vaoIndex].VertexBufferId = vertexBufferId;
                _vertexArrayObjects[vaoIndex].IndexBufferId = indexBufferId;

                GL.GenVertexArrays(1, out _vertexArrayObjects[vaoIndex].VertexArrayObjectId);

                GL.BindVertexArray(_vertexArrayObjects[vaoIndex].VertexArrayObjectId);

                var bufferHandle = _bufferManager.GetOpenGLHandle(_vertexArrayObjects[vaoIndex].VertexBufferId);
                var vertexFormat = _bufferManager.GetVertexFormat(_vertexArrayObjects[vaoIndex].VertexBufferId);

                GL.Ext.VertexArrayBindVertexBuffer(_vertexArrayObjects[vaoIndex].VertexArrayObjectId, 0, bufferHandle, IntPtr.Zero, vertexFormat.Size);
                SetVertexFormat(_vertexArrayObjects[vaoIndex].VertexArrayObjectId, 0, vertexFormat);

                _bufferManager.Bind(_vertexArrayObjects[vaoIndex].IndexBufferId);

                GL.BindVertexArray(_activeMeshHandle);
            }

            _handles[index].VertexArrayObjectIndex = vaoIndex;
            _handles[index].Initialized = true;
        }

        public void SetTriangleCount(int handle, int triangleCount)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id)
                return;

            _handles[index].PrimitiveCount = triangleCount * 3;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Render(int handle, PrimitiveType primitiveType)
        {
            ExtractHandle(handle, out var index, out var id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
            {
                return;
            }

            var vao = _vertexArrayObjects[_handles[index].VertexArrayObjectIndex].VertexArrayObjectId;
            if (_activeMeshHandle != vao)
            {
                GL.BindVertexArray(vao);
                _activeMeshHandle = vao;
            }

            var offset = _bufferManager.GetOffset(_handles[index].IndexBufferId);

            var baseVertex = _bufferManager.GetOffset(_handles[index].VertexBufferId);
            var vertexFormat = _bufferManager.GetVertexFormat(_handles[index].VertexBufferId);

            baseVertex /= vertexFormat.Size;

            GL.DrawElementsBaseVertex(primitiveType, _handles[index].PrimitiveCount, (DrawElementsType)_handles[index].Type, (IntPtr)offset, baseVertex);
        }

        public unsafe void Render(DrawMeshMultiData* meshIndices, int count)
        {
            // Prepare indirect buffer

            var vao = -1; DrawElementsType drawElementsType = DrawElementsType.UnsignedByte; VertexFormat vertexFormat = null;
            for (var i = 0; i < count; i++)
            {
                ExtractHandle(meshIndices[i].MeshHandle, out var index, out var id);

                if (vao == -1)
                {
                    vao = _vertexArrayObjects[_handles[index].VertexArrayObjectIndex].VertexArrayObjectId;
                    drawElementsType = (DrawElementsType)_handles[index].Type;
                    vertexFormat = _bufferManager.GetVertexFormat(_handles[index].VertexBufferId);
                }

                if (vao != _vertexArrayObjects[_handles[index].VertexArrayObjectIndex].VertexArrayObjectId)
                {
                    throw new InvalidOperationException();
                }

                var offset = _bufferManager.GetOffset(_handles[index].IndexBufferId);

                var baseVertex = _bufferManager.GetOffset(_handles[index].VertexBufferId);
                baseVertex /= vertexFormat.Size;

                _drawIndirectCommands[i].Count = (uint)_handles[index].PrimitiveCount;
                _drawIndirectCommands[i].InstanceCount = 1;
                _drawIndirectCommands[i].FirstIndex = (uint)(offset / sizeof(uint));
                _drawIndirectCommands[i].BaseVertex = (uint)baseVertex;
                _drawIndirectCommands[i].BaseInstance = (uint)meshIndices[i].BaseInstance;
            }

            // Upload indirect buffer
            GL.InvalidateBufferData(_indirectBufferHandles[_currentIndirectBuffer]);
            fixed (DrawElementsIndirectCommand* draw = _drawIndirectCommands)
            {
                GL.NamedBufferData(_indirectBufferHandles[_currentIndirectBuffer], sizeof(DrawElementsIndirectCommand) * count, (IntPtr)draw, BufferUsageHint.StreamDraw);
            }
            
            // Draw
            if (_activeMeshHandle != vao)
            {
                GL.BindVertexArray(vao);
                _activeMeshHandle = vao;
            }

            GL.BindBuffer(OGL.BufferTarget.DrawIndirectBuffer, _indirectBufferHandles[_currentIndirectBuffer]);
            GL.MultiDrawElementsIndirect(PrimitiveType.Triangles, drawElementsType, IntPtr.Zero, count, 0);
            GL.BindBuffer(OGL.BufferTarget.DrawIndirectBuffer, 0);

            _currentIndirectBuffer = ++_currentIndirectBuffer % _indirectBufferHandles.Length; // Advance ring buffer
        }

        public void Render(int handle, PrimitiveType primitiveType, int offset, int count)
        {
            ExtractHandle(handle, out var index, out var id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
            {
                return;
            }

            var vao = _vertexArrayObjects[_handles[index].VertexArrayObjectIndex].VertexArrayObjectId;
            if (_activeMeshHandle != vao)
            {
                GL.BindVertexArray(vao);
                _activeMeshHandle = vao;
            }

            GL.DrawElements(primitiveType, count, (DrawElementsType)_handles[index].Type, offset);
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

            vertexBufferId = _handles[index].VertexBufferId;
            indexBufferId = _handles[index].IndexBufferId;

            return true;
        }

        struct MeshData
        {
            public bool Initialized;
            public short Id;

            public int VertexArrayObjectIndex;
            public int VertexBufferId;
            public int IndexBufferId;
            public int PrimitiveCount;
            public IndexType Type;
        }

        struct VertexArrayObjects
        {
            public int VertexArrayObjectId;
            public int VertexBufferId;
            public int IndexBufferId;
            public int VertexBufferGLHandle;
            public int IndexBufferGLHandle;
            public IndexType Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DrawElementsIndirectCommand
        {
            public uint Count;
            public uint InstanceCount;
            public uint FirstIndex;
            public uint BaseVertex;
            public uint BaseInstance;
        }
    }
}
