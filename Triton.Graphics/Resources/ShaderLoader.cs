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
			return new ShaderProgram(name, parameters, Backend);
		}

		public void Load(Common.Resource resource, string parameters)
		{
			if (resource.IsLoaded && resource.Parameters == parameters)
				return;

			var shader = (ShaderProgram)resource;

			// This will reset some cache data like uniform locations
			shader.Reset();

			var filename = resource.Name + ".glsl";

			var shaderSource = ""; // Complete source of both shaders before splitting them

			using (var stream = FileSystem.OpenRead(filename))
			using (var reader = new System.IO.StreamReader(stream))
			{
				shaderSource = reader.ReadToEnd();
			}

			var preProcessor = new Shaders.Preprocessor(FileSystem);

			Shaders.Attrib[] shaderAttribs;
			Shaders.Uniform[] uniforms;
			shaderSource = preProcessor.Process(shaderSource, out shaderAttribs, out uniforms);

			var vertexShaderSource = "#version 150\n#define VERTEX_SHADER\n" + shaderSource;
			var fragmentShaderSource = "#version 150\n#define FRAGMENT_SHADER\n" + shaderSource;

			// Convert attribs to the correct format
			// The format is attribIndex => name
			string[] attribs = new string[4];
			for (var i = 0; i < shaderAttribs.Length; i++)
			{
				var attrib = shaderAttribs[i];
				attribs[(int)attrib.Type] = attrib.Name;
			}

			// Setup uniforms, the bind locations wont be resolved until they are used
			for (var i = 0; i < uniforms.Length; i++)
			{
				shader.AddUniform(uniforms[i].BindName, uniforms[i].Name);
			}

			if (shader.Handle == -1)
				shader.Handle = Backend.RenderSystem.CreateShader(vertexShaderSource, fragmentShaderSource, attribs, OnError);
			else
				Backend.RenderSystem.SetShaderData(shader.Handle, vertexShaderSource, fragmentShaderSource, attribs, OnError);

			resource.Parameters = parameters;
			resource.IsLoaded = true;
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
