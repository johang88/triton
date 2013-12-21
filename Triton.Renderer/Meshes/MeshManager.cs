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

		public MeshManager()
		{
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
					GL.DeleteBuffers(2, new int[] { Handles[i].VertexBufferID, Handles[i].IndexBufferID });
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
				GL.DeleteBuffers(2, new int[] { Handles[index].VertexBufferID, Handles[index].IndexBufferID });
				GL.DeleteVertexArrays(1, ref Handles[index].VertexArrayObjectID);
			}

			Handles[index].Initialized = false;
		}

		public void SetData<T, T2>(int handle, VertexFormat vertexFormat, int triangleCount, T[] vertexData, T2[] indexData, bool stream)
			where T : struct
			where T2 : struct
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			InitBuffers(index, vertexFormat);

			// Load vertex data
			GL.BindBuffer(BufferTarget.ArrayBuffer, Handles[index].VertexBufferID);
			if (vertexData != null)
				GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertexData.Length * Marshal.SizeOf(typeof(T))), vertexData, stream ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw);

			// Load index data
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, Handles[index].IndexBufferID);
			if (indexData != null)
				GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indexData.Length * Marshal.SizeOf(typeof(T2))), indexData, stream ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw);
			ClearBindings();

			Handles[index].TriangleCount = triangleCount;
		}

		private void ClearBindings()
		{
			GL.BindVertexArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		private void InitBuffers(int index, VertexFormat vertexFormat)
		{
			if (!Handles[index].Initialized)
			{
				GL.GenVertexArrays(1, out Handles[index].VertexArrayObjectID);
				GL.BindVertexArray(Handles[index].VertexArrayObjectID);

				GL.GenBuffers(1, out Handles[index].VertexBufferID);
				GL.GenBuffers(1, out Handles[index].IndexBufferID);

				GL.BindBuffer(BufferTarget.ArrayBuffer, Handles[index].VertexBufferID);
				SetVertexFormat(vertexFormat);

				Handles[index].Initialized = true;
			}
		}

		private void SetVertexFormat(VertexFormat vertexFormat)
		{
			for (var i = 0; i < vertexFormat.Elements.Length; i++)
			{
				var element = vertexFormat.Elements[i];
				var index = (int)element.Semantic;

				GL.EnableVertexAttribArray(index);
				GL.VertexAttribPointer(index, element.Count, (VertexAttribPointerType)(int)element.Type, false, vertexFormat.Size, element.Offset);
			}
		}

		public void GetMeshData(int handle, out int triangleCount, out int vertexArrayObjectId, out int indexBufferId)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
			{
				triangleCount = vertexArrayObjectId = indexBufferId = -1;
				return;
			}

			triangleCount = Handles[index].TriangleCount;
			vertexArrayObjectId = Handles[index].VertexArrayObjectID;
			indexBufferId = Handles[index].IndexBufferID;
		}

		struct MeshData
		{
			public bool Initialized;
			public short Id;

			public int VertexBufferID;
			public int IndexBufferID;
			public int VertexArrayObjectID;
			public int TriangleCount;
		}
	}
}
