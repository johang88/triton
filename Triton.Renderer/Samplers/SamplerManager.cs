using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OGL = OpenTK.Graphics.OpenGL;

namespace Triton.Renderer.Samplers
{
	class SamplerManager : IDisposable
	{
		const int MaxHandles = 64;
		private readonly SamplerData[] Handles = new SamplerData[MaxHandles];
		private short NextFree = 0;
		private bool Disposed = false;
		private readonly object Lock = new object();

		private readonly int[] ActiveSamplers;

		public SamplerManager()
		{
			// Each empty handle will store the location of the next empty handle 
			for (var i = 0; i < Handles.Length; i++)
			{
				Handles[i].Id = (short)(i + 1);
			}

			Handles[Handles.Length - 1].Id = -1;

			ActiveSamplers = new int[20];
			for (var i = 0; i < ActiveSamplers.Length; i++)
			{
				ActiveSamplers[i] = -1;
			}
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
					GL.DeleteSampler(Handles[i].SamplerObject);
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

			return CreateHandle(index, id);
		}

		public void Init(int handle, Dictionary<SamplerParameterName, int> settings)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			Handles[index].SamplerObject = GL.GenSampler();
			int sampler = Handles[index].SamplerObject;

			foreach (var setting in settings)
			{
				GL.SamplerParameter(sampler, (OGL.SamplerParameterName)(int)setting.Key, setting.Value);
			}

			Handles[index].Initialized = true;
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
			{
				GL.DeleteSampler(Handles[index].SamplerObject);
			}

			Handles[index].Initialized = false;
		}

		public void Bind(int textureUnit, int handle)
		{
			if (ActiveSamplers[textureUnit] == handle)
				return;

			ActiveSamplers[textureUnit] = handle;

			int sampler = GetOpenGLHande(handle);
			GL.BindSampler(textureUnit, sampler);
		}

		public int GetOpenGLHande(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
			{
				return 0;
			}

			return Handles[index].SamplerObject;
		}

		struct SamplerData
		{
			public short Id;
			public bool Initialized;

			public int SamplerObject;
		}
	}
}
