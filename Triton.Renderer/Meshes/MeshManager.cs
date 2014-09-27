using OpenTK.Graphics.OpenGL;
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
		private readonly MeshData[] Handles = new MeshData[MaxHandles];
		private int NextFree = 0;
		private bool Disposed = false;
		private readonly object Lock = new object();
		private readonly BufferManager BufferManager;
		private int ActiveMeshHandle = 0;

		public MeshManager(BufferManager bufferManager)
		{
			BufferManager = bufferManager;

			// Each empty handle will store the location of the next empty handle 
			for (var i = 0; i < Handles.Length; i++)
			{
				Handles[i].Id = (short)(i + 1);
			}

			Handles[Handles.Length - 1].Id = -1;
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
				{
					GL.DeleteVertexArrays(1, ref Handles[i].VertexArrayObjectID);
				}
				Handles[i].Initialized = false;
			}

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
				CreateHandle(-1, -1);
			}

			int index;
			lock (Lock)
			{
				index = NextFree;
				NextFree = Handles[NextFree].Id;
			}

			var id = ++Handles[index].Id;
			Handles[index].Initialized = false;
			Handles[index].VertexBufferID = null;
			Handles[index].IndexBufferID = -1;

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
				Handles[index].Id = (short)NextFree;
				NextFree = index;
			}

			if (Handles[index].Initialized)
			{
				GL.DeleteVertexArrays(1, ref Handles[index].VertexArrayObjectID);
			}

			Handles[index].Initialized = false;
		}

		public void Initialize(int handle, int triangleCount, int vertexBufferId, int indexBufferId)
		{
			Initialize(handle, triangleCount, new int[] { vertexBufferId }, indexBufferId);
		}

		public void Initialize(int handle, int triangleCount, int[] vertexBufferId, int indexBufferId)
		{
			if (vertexBufferId.Length == 0)
				throw new ArgumentException("missing vertex buffer");

			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			if (Handles[index].Initialized)
				return;

			Handles[index].VertexBufferID = vertexBufferId;
			Handles[index].IndexBufferID = indexBufferId;
			Handles[index].TriangleCount = triangleCount;

			GL.GenVertexArrays(1, out Handles[index].VertexArrayObjectID);
			GL.BindVertexArray(Handles[index].VertexArrayObjectID);

			for (var i = 0; i < vertexBufferId.Length; i++)
			{
				BufferManager.Bind(Handles[index].VertexBufferID[i]);
				SetVertexFormat(BufferManager.GetVertexFormat(vertexBufferId[i]));
			}

			BufferManager.Bind(Handles[index].IndexBufferID);

			GL.BindVertexArray(0);
			ActiveMeshHandle = 0;

			BufferManager.Unbind(Handles[index].VertexBufferID[0]);
			BufferManager.Unbind(Handles[index].IndexBufferID);

			Handles[index].Initialized = true;
		}

		public void SetTriangleCount(int handle, int triangleCount)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			Handles[index].TriangleCount = triangleCount;
		}

		public void SetIndexBuffer(int handle, int indexBufferId)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			GL.BindVertexArray(Handles[index].VertexArrayObjectID);
			Handles[index].IndexBufferID = indexBufferId;

			BufferManager.Bind(Handles[index].IndexBufferID);

			GL.BindVertexArray(0);
			ActiveMeshHandle = 0;
			BufferManager.Unbind(Handles[index].IndexBufferID);
		}

		private void SetVertexFormat(VertexFormat vertexFormat)
		{
			for (var i = 0; i < vertexFormat.Elements.Length; i++)
			{
				var element = vertexFormat.Elements[i];
				var index = (int)element.Semantic;

				GL.EnableVertexAttribArray(index);
				GL.VertexAttribPointer(index, element.Count, (VertexAttribPointerType)(int)element.Type, false, vertexFormat.Size, element.Offset);
				GL.VertexAttribDivisor(index, element.Divisor);
			}
		}

		public void Render(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
			{
				return;
			}

			if (ActiveMeshHandle != handle)
			{
				GL.BindVertexArray(Handles[index].VertexArrayObjectID);
				ActiveMeshHandle = handle;
			}

			GL.DrawElements(PrimitiveType.Triangles, Handles[index].TriangleCount * 3, DrawElementsType.UnsignedInt, IntPtr.Zero);
		}

		public void GetRenderData(int handle, out int triangleCount, out int vertexArrayObjectId)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
			{
				triangleCount = vertexArrayObjectId = -1;
				return;
			}

			triangleCount = Handles[index].TriangleCount;
			vertexArrayObjectId = Handles[index].VertexArrayObjectID;
		}

		public bool GetMeshData(int handle, out int vertexBufferId, out int indexBufferId)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
			{
				vertexBufferId = indexBufferId = -1;
				return false;
			}

			vertexBufferId = Handles[index].VertexBufferID[0];
			indexBufferId = Handles[index].IndexBufferID;

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
		}
	}
}
