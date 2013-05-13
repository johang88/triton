using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	/// <summary>
	/// Resource loader for shader programs
	/// Shaders are defined in a single glsl file, the defines VERTEX_SHADER and FRAGMENT_SHADER
	/// are used to differ between the compiled shader type. 
	/// 
	/// Various pragmas and preprocessor defines are also setup.
	/// 
	/// </summary>
	class ShaderLoader : Triton.Common.IResourceLoader<ShaderProgram>
	{
		private readonly Backend Backend;
		private readonly Triton.Common.IO.FileSystem FileSystem;

		public ShaderLoader(Backend backend, Triton.Common.IO.FileSystem fileSystem)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			Backend = backend;
			FileSystem = fileSystem;

		}

		public Common.Resource Create(string name, string parameters)
		{
			return new ShaderProgram(name, parameters);
		}

		public void Load(Common.Resource resource, string parameters)
		{
			if (resource.IsLoaded && resource.Parameters == parameters)
				return;

			var shader = (ShaderProgram)resource;

			var filename = resource.Name + ".glsl";

			var shaderSource = ""; // Complete source of both shaders before splitting them

			using (var stream = FileSystem.OpenRead(filename))
			using (var reader = new System.IO.StreamReader(stream))
			{
				shaderSource = reader.ReadToEnd();
			}

			var vertexShaderSource = "#define VERTEX_SHADER\n" + shaderSource;
			var fragmentShaderSource = "#define FRAGMENT_SHADER\n" + shaderSource;

			var attribs = parameters.Split(',');

			if (shader.Handle == -1)
				shader.Handle = Backend.RenderSystem.CreateShader(vertexShaderSource, fragmentShaderSource, attribs, OnError);
			else
				Backend.RenderSystem.SetShaderData(shader.Handle, vertexShaderSource, fragmentShaderSource, attribs, OnError);
		}

		void OnError(int shaderHandle, string errors)
		{
			Console.WriteLine(errors);
		}

		public void Unload(Common.Resource resource)
		{
			resource.IsLoaded = false;
			var shader = (ShaderProgram)resource;
			Backend.RenderSystem.DestroyShader(shader.Handle);
			shader.Handle = -1;
		}
	}
}
