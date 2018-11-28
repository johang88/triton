using OpenTK.Graphics.OpenGL;
using OGL = OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Utility;

namespace Triton.Renderer.Shaders
{
	class ShaderManager : IDisposable
	{
		const int MaxHandles = 4096;
		private readonly ShaderData[] Handles = new ShaderData[MaxHandles];
		private short NextFree = 0;
		private bool Disposed = false;
		private readonly object Lock = new object();

		private int ActiveShaderHandle = -1;

		public ShaderManager()
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
					foreach (var handle in Handles[i].ShaderHandles)
					{
						GL.DeleteShader(handle);
					}
					GL.DeleteProgram(Handles[i].ProgramHandle);
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
			Handles[index].ShaderHandles = null;
			Handles[index].ProgramHandle = 0;

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
				Handles[index].Id = NextFree;
				NextFree = (short)index;
			}

			if (Handles[index].Initialized)
			{
				foreach (var shaderHandle in Handles[index].ShaderHandles)
				{
					GL.DeleteShader(shaderHandle);
				}
				GL.DeleteProgram(Handles[index].ProgramHandle);
			}

			Handles[index].Initialized = false;
		}

		public bool SetShaderData(int handle, Dictionary<ShaderType, string> sources, out string errors)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
			{
				errors = "Invalid handle";
				return false;
			}

			errors = "";

			if (!Handles[index].Initialized)
			{
				Handles[index].ShaderHandles = new int[sources.Count];
				Handles[index].ProgramHandle = GL.CreateProgram();
			}

			int errorCode = 1;
			int i = 0;
			foreach (var shader in sources)
			{
				if (Handles[index].ShaderHandles[i] == 0)
				{
					Handles[index].ShaderHandles[i] = GL.CreateShader((OGL.ShaderType)(int)shader.Key);
				}
				GL.ShaderSource(Handles[index].ShaderHandles[i], shader.Value);
				GL.CompileShader(Handles[index].ShaderHandles[i]);

				GL.GetShader(Handles[index].ShaderHandles[i], ShaderParameter.CompileStatus, out errorCode);
				GL.GetShaderInfoLog(Handles[index].ShaderHandles[i], out errors);

				if (errorCode != 1)
					break;

				i++;
			}


			if (errorCode != 1)
			{
				foreach (var shaderHandle in Handles[index].ShaderHandles)
				{
					GL.DeleteShader(shaderHandle);
				}

				GL.DeleteProgram(Handles[index].ProgramHandle);

				Handles[index].Initialized = false;

				return false;
			}

			// Link program
			foreach (var shaderHandle in Handles[index].ShaderHandles)
			{
				GL.AttachShader(Handles[index].ProgramHandle, shaderHandle);
			}

			GL.LinkProgram(Handles[index].ProgramHandle);

			// Check for link errors

			GL.GetProgram(Handles[index].ProgramHandle, GetProgramParameterName.LinkStatus, out errorCode);
			if (errorCode != 1)
			{
				GL.GetProgramInfoLog(Handles[index].ProgramHandle, out errors);

				foreach (var shaderHandle in Handles[index].ShaderHandles)
				{
					GL.DeleteShader(shaderHandle);
				}

				GL.DeleteProgram(Handles[index].ProgramHandle);

				Handles[index].Initialized = false;

				return false;
			}

			// The shader program can now be used
			Handles[index].Initialized = true;

			return true;
		}

		public Dictionary<HashedString, int> GetUniforms(int handle)
		{
			var program = GetOpenGLHande(handle);
			var uniforms = new Dictionary<HashedString, int>();

			int uniformCount;
			GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out uniformCount);

			for (var i = 0; i < uniformCount; i++)
			{
				int size;
				ActiveUniformType type;

				var name = GL.GetActiveUniform(program, i, out size, out type);
				name = name.Replace("[0]", "");
				var location = GL.GetUniformLocation(program, name);

				uniforms.Add(name, location);
			}

			return uniforms;
		}

		public void Bind(int handle)
		{
			if (ActiveShaderHandle == handle)
				return;

			ActiveShaderHandle = handle;

			var programHandle = GetOpenGLHande(ActiveShaderHandle);
			GL.UseProgram(programHandle);
		}

		public int GetOpenGLHande(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
				return 0;

			return Handles[index].ProgramHandle;
		}

		struct ShaderData
		{
			public int ProgramHandle;
			public int[] ShaderHandles;
			public short Id;
			public bool Initialized;
		}
	}
}
