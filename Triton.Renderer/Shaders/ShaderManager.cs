using OpenTK.Graphics.OpenGL;
using OGL = OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer.Shaders
{
	class ShaderManager : IDisposable
	{
		const int MaxHandles = 4096;
		private readonly ShaderData[] Handles = new ShaderData[MaxHandles];
		private short NextFree = 0;
		private bool Disposed = false;
		private readonly object Lock = new object();

		int DefaultVertexHandle;
		int DefaultFragmentHandle;
		int DefaultProgramHandle;

		public ShaderManager()
		{
			// Each empty handle will store the location of the next empty handle 
			for (var i = 0; i < Handles.Length; i++)
			{
				Handles[i].Id = (short)(i + 1);
			}

			Handles[Handles.Length - 1].Id = -1;

			CreateDefaultHandles();
		}

		void CreateDefaultHandles()
		{
			DefaultVertexHandle = GL.CreateShader(ShaderType.VertexShader);
			DefaultFragmentHandle = GL.CreateShader(ShaderType.FragmentShader);

			GL.ShaderSource(DefaultVertexHandle, DefaultVertexShaderSource);
			GL.ShaderSource(DefaultFragmentHandle, DefaultFragmentShaderSource);

			GL.CompileShader(DefaultVertexHandle);
			GL.CompileShader(DefaultFragmentHandle);

			DefaultProgramHandle = GL.CreateProgram();

			GL.AttachShader(DefaultProgramHandle, DefaultVertexHandle);
			GL.AttachShader(DefaultProgramHandle, DefaultFragmentHandle);

			GL.BindAttribLocation(DefaultProgramHandle, 0, "iPosition");
			GL.BindAttribLocation(DefaultProgramHandle, 3, "iTexCoord");

			GL.LinkProgram(DefaultProgramHandle);
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
					GL.DeleteShader(Handles[i].VertexHandle);
					GL.DeleteShader(Handles[i].FragmentHandle);
					if (Handles[i].GeometryHandle != 0)
						GL.DeleteShader(Handles[i].GeometryHandle);
					GL.DeleteProgram(Handles[i].ProgramHandle);
				}
				Handles[i].Initialized = false;
			}

			GL.DeleteShader(DefaultVertexHandle);
			GL.DeleteShader(DefaultFragmentHandle);
			GL.DeleteProgram(DefaultProgramHandle);

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
			Handles[index].VertexHandle = 0;
			Handles[index].FragmentHandle = 0;
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
				GL.DeleteShader(Handles[index].VertexHandle);
				GL.DeleteShader(Handles[index].FragmentHandle);
				if (Handles[index].GeometryHandle != 0)
					GL.DeleteShader(Handles[index].GeometryHandle);
				GL.DeleteProgram(Handles[index].ProgramHandle);
			}

			Handles[index].Initialized = false;
		}

		public bool SetShaderData(int handle, string vertexShader, string fragmentShader, string geometryShader, string[] attribs, string[] fragDataLocations, out string errors)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
			{
				errors = "Invalid handle";
				return false;
			}

			if (!Handles[index].Initialized)
			{
				Handles[index].VertexHandle = GL.CreateShader(ShaderType.VertexShader);
				Handles[index].FragmentHandle = GL.CreateShader(ShaderType.FragmentShader);

				if (!string.IsNullOrEmpty(geometryShader))
					Handles[index].GeometryHandle = GL.CreateShader(ShaderType.GeometryShader);
				else
					Handles[index].GeometryHandle = 0;

				Handles[index].ProgramHandle = GL.CreateProgram();
			}

			// Compile shaders

			GL.ShaderSource(Handles[index].VertexHandle, vertexShader);
			GL.ShaderSource(Handles[index].FragmentHandle, fragmentShader);

			if (!string.IsNullOrEmpty(geometryShader))
			{
				GL.ShaderSource(Handles[index].GeometryHandle, fragmentShader);
			}

			GL.CompileShader(Handles[index].VertexHandle);
			GL.CompileShader(Handles[index].FragmentHandle);

			// Check for compilation errors
			int errorCode;

			GL.GetShader(Handles[index].VertexHandle, ShaderParameter.CompileStatus, out errorCode);
			GL.GetShaderInfoLog(Handles[index].VertexHandle, out errors);

			if (errorCode == 1)
			{
				GL.GetShader(Handles[index].FragmentHandle, ShaderParameter.CompileStatus, out errorCode);
				GL.GetShaderInfoLog(Handles[index].FragmentHandle, out errors);
			}

			if (errorCode != 1)
			{
				// Clean up the shader stuff so we don't risk a leak :)
				GL.DeleteShader(Handles[index].VertexHandle);
				GL.DeleteShader(Handles[index].FragmentHandle);
				if (Handles[index].GeometryHandle != 0)
					GL.DeleteShader(Handles[index].GeometryHandle);
				GL.DeleteProgram(Handles[index].ProgramHandle);

				Handles[index].Initialized = false;

				return false;
			}

			// Link program
			GL.AttachShader(Handles[index].ProgramHandle, Handles[index].VertexHandle);
			GL.AttachShader(Handles[index].ProgramHandle, Handles[index].FragmentHandle);

			for (var i = 0; i < attribs.Length; i++)
			{
				var attribName = attribs[i];
				if (string.IsNullOrWhiteSpace(attribName))
					continue;

				GL.BindAttribLocation(Handles[index].ProgramHandle, i, attribName);
			}

			for (var i = 0; i < fragDataLocations.Length; i++)
			{
				var fragDataLocationName = fragDataLocations[i];
				if (string.IsNullOrWhiteSpace(fragDataLocationName))
					continue;

				GL.BindFragDataLocation(Handles[index].ProgramHandle, i, fragDataLocationName);
			}

			GL.LinkProgram(Handles[index].ProgramHandle);

			// Check for link errors

			GL.GetProgram(Handles[index].ProgramHandle, ProgramParameter.LinkStatus, out errorCode);
			if (errorCode != 1)
			{
				GL.GetProgramInfoLog(Handles[index].ProgramHandle, out errors);

				// Clean up the shader stuff so we don't risk a leak :)
				GL.DeleteShader(Handles[index].VertexHandle);
				GL.DeleteShader(Handles[index].FragmentHandle);
				GL.DeleteProgram(Handles[index].ProgramHandle);

				Handles[index].Initialized = false;

				return false;
			}

			// The shader program can now be used
			Handles[index].Initialized = true;

			return true;
		}

		public int GetOpenGLHande(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
				return DefaultProgramHandle;

			return Handles[index].ProgramHandle;
		}

		struct ShaderData
		{
			public int ProgramHandle;
			public int VertexHandle;
			public int FragmentHandle;
			public int GeometryHandle;
			public short Id;
			public bool Initialized;
		}

		const string DefaultVertexShaderSource
			= "#version 150\n"
			+ "in vec3 iPosition;\n"
			+ "in vec2 iTexCoord;\n"
			+ "out vec2 texCoord;\n"
			+ "uniform mat4x4 modelViewProjection;\n"
			+ "void main() {\n"
			+ "    texCoord = iTexCoord;\n"
			+ "    gl_Position = modelViewProjection * vec4(iPosition, 1);\n"
			+ "}";

		const string DefaultFragmentShaderSource
			= "#version 150\n"
			+ "in vec2 texCoord;\n"
			+ "out vec4 oColor;\n"
			+ "uniform sampler2D samplerDiffuse;\n"
			+ "void main() {\n"
			+ "    oColor = texture2D(samplerDiffuse, texCoord);\n"
			+ "}";
	}
}
