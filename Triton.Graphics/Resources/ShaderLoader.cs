using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
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

			var vertexShaderName = resource.Name + ".v.glsl";
			var fragmentShaderName = resource.Name + ".f.glsl";

			string vertexShaderSource = "";
			string fragmentShaderSource = "";

			using (var stream = FileSystem.OpenRead(vertexShaderName))
			using (var reader = new System.IO.StreamReader(stream))
			{
				vertexShaderSource = reader.ReadToEnd();
			}

			using (var stream = FileSystem.OpenRead(vertexShaderName))
			using (var reader = new System.IO.StreamReader(stream))
			{
				fragmentShaderSource = reader.ReadToEnd();
			}

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
