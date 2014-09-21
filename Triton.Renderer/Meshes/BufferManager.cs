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
		private readonly BufferData[] Handles = new BufferData[MaxHandles];
		private int NextFree = 0;
		private bool Disposed = false;
		private readonly object Lock = new object();

		public BufferManager()
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
					GL.DeleteBuffers(1, new int[] { Handles[i].BufferID });
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

		public int Create(BufferTarget target, VertexFormat vertexFormat = null)
		{
			if (target == BufferTarget.ArrayBuffer && vertexFormat == null)
				throw new Exception("missing vertex format");

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
			Handles[index].Target = (OGL.BufferTarget)(int)target;
			Handles[index].VertexFormat = vertexFormat;

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
				GL.DeleteBuffers(1, new int[] { Handles[index].BufferID });
			}

			Handles[index].Initialized = false;
		}

		public void SetDataDirect(int handle, IntPtr dataLength, IntPtr data, bool stream)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
				return;

			if (data == IntPtr.Zero || data == null)
				return;	

			GL.BindBuffer(Handles[index].Target, Handles[index].BufferID);
			GL.BufferData(Handles[index].Target, dataLength, data, stream ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw);

			GL.BindBuffer(Handles[index].Target, 0);
		}

		public void SetData<T>(int handle, T[] data, bool stream)
			where T : struct
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			if (data == null)
				return;

			if (!Handles[index].Initialized)
			{
				GL.GenBuffers(1, out Handles[index].BufferID);
			}

			GL.BindBuffer(Handles[index].Target, Handles[index].BufferID);
			GL.BufferData(Handles[index].Target, new IntPtr(data.Length * Marshal.SizeOf(typeof(T))), data, stream ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw);

			Handles[index].Initialized = true;

			GL.BindBuffer(Handles[index].Target, 0);
		}

		public void GetOpenGLHandle(int handle, out int glHandle, out OGL.BufferTarget target)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
			{
				glHandle = -1;
				target = OGL.BufferTarget.ArrayBuffer;
				return;
			}

			glHandle = Handles[index].BufferID;
			target = Handles[index].Target;
		}

		public VertexFormat GetVertexFormat(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return null;

			return Handles[index].VertexFormat;
		}

		public void Bind(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
				return;

			GL.BindBuffer(Handles[index].Target, Handles[index].BufferID);
		}

		public void Unbind(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
				return;

			GL.BindBuffer(Handles[index].Target, 0);
		}

		struct BufferData
		{
			public bool Initialized;
			public short Id;

			public int BufferID;
			public OGL.BufferTarget Target;
			public VertexFormat VertexFormat;
		}
	}
}
