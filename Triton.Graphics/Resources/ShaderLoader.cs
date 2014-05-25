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
		private readonly Dictionary<string, ShaderProgram> Shaders = new Dictionary<string, ShaderProgram>();
		private object ReloadLock = new object();

		public ShaderLoader(Backend backend, Triton.Common.IO.FileSystem fileSystem)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			Backend = backend;
			FileSystem = fileSystem;
		}

		private void OnFileChanged(string path)
		{
			if (!path.EndsWith(".glsl"))
				return;

			path = path.Replace(".glsl", "");

			lock (ReloadLock)
			{
				ShaderProgram shader;
				if (Shaders.TryGetValue(path, out shader) && shader.State == Common.ResourceLoadingState.Loaded)
				{
					Load(shader, "", null);
				}
			}
		}

		public Common.Resource Create(string name, string parameters)
		{
			return new ShaderProgram(name, parameters, Backend);
		}

		public void Load(Common.Resource resource, string parameters, Action<Common.Resource> onLoaded)
		{
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

			shaderSource = preProcessor.Process(shaderSource);

			var defines = "";

			if (!string.IsNullOrWhiteSpace(parameters))
			{
				var definesBuilder = new StringBuilder();
				foreach (var param in parameters.Split(','))
				{
					definesBuilder.AppendLine("#define " + param);
				}

				defines = definesBuilder.ToString();
			}

			var vertexShaderSource = "#version 330 core\n#define VERTEX_SHADER\n" + defines + "\n" + shaderSource;
			var fragmentShaderSource = "#version 330 core\n#define FRAGMENT_SHADER\n" + defines + "\n" + shaderSource;

			var geometryShaderSource = "";
			if (shaderSource.Contains("GEOMETRY_SHADER"))
			{
				geometryShaderSource = "#version 330 core\n#define GEOMETRY_SHADER\n" + defines + "\n" + shaderSource;
			}

			resource.Parameters = parameters;

			Renderer.RenderSystem.OnLoadedCallback onResourceLoaded = (handle, success, errors) =>
			{
				if (success)
				{
					shader.Uniforms = Backend.RenderSystem.GetUniforms(shader.Handle);
				}

				if (!string.IsNullOrWhiteSpace(errors))
					Common.Log.WriteLine(shader.Name + ": " + errors, success ? Common.LogLevel.Default : Common.LogLevel.Error);

				if (!Shaders.ContainsKey(shader.Name))
					Shaders.Add(shader.Name, shader);

				if (onLoaded != null)
					onLoaded(resource);
			};

			if (shader.Handle == -1)
				shader.Handle = Backend.RenderSystem.CreateShader(vertexShaderSource, fragmentShaderSource, geometryShaderSource, onResourceLoaded);
			else
				Backend.RenderSystem.SetShaderData(shader.Handle, vertexShaderSource, fragmentShaderSource, geometryShaderSource, onResourceLoaded);
		}

		public void Unload(Common.Resource resource)
		{
			var shader = (ShaderProgram)resource;
			Backend.RenderSystem.DestroyShader(shader.Handle);
			shader.Handle = -1;
		}
	}
}
