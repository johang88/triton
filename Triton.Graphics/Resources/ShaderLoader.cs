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

		public string Extension { get { return ".glsl"; } }
		public string DefaultFilename { get { return ""; } }

		private void OutputShader(string name, string type, string parameters, string content)
		{
			if (!string.IsNullOrWhiteSpace(content) && FileSystem.DirectoryExists("/tmp/"))
			{
				parameters = parameters.Replace(',', '_').Replace('/', '_').Replace('\\', '_');
				if (!string.IsNullOrWhiteSpace(parameters))
					parameters = '-' + parameters;
				var filename = name.Replace('/', '_') + parameters + '.' + type;
				using (var stream = FileSystem.OpenWrite("/tmp/" + filename))
				using (var writer = new System.IO.StreamWriter(stream))
				{
					writer.Write(content);
				}
			}
		}

		public object Create(string name, string parameters)
		{
			return new ShaderProgram(name, parameters, Backend);
		}

		private string InsertHeader(string type, string defines, string source)
		{
			return string.Format("#version 410 core\n#define {0}\n{1}\n", type, defines) + source;
		}

		public void Load(object resource, byte[] data)
		{
			var shader = (ShaderProgram)resource;

			// This will reset some cache data like uniform locations
			shader.Reset();

			var filename = shader.Name + ".glsl";

			var shaderSource = Encoding.ASCII.GetString(data); // Complete source of both shaders before splitting them

			var preProcessor = new Shaders.Preprocessor(FileSystem);

			shaderSource = preProcessor.Process(shaderSource);

			var defines = "";

			var parameters = shader.Parameters;

			if (!string.IsNullOrWhiteSpace(parameters))
			{
				var definesBuilder = new StringBuilder();
				foreach (var param in parameters.Split(';'))
				{
					definesBuilder.AppendLine("#define " + param);
				}

				defines = definesBuilder.ToString();
			}

			var sources = new Dictionary<Renderer.ShaderType, string>();

			if (shaderSource.Contains("VERTEX_SHADER"))
			{
				sources.Add(Renderer.ShaderType.VertexShader, InsertHeader("VERTEX_SHADER", defines, shaderSource));
				sources.Add(Renderer.ShaderType.FragmentShader, InsertHeader("FRAGMENT_SHADER", defines, shaderSource));
			}

			if (shaderSource.Contains("GEOMETRY_SHADER"))
			{
				sources.Add(Renderer.ShaderType.GeometryShader, InsertHeader("GEOMETRY_SHADER", defines, shaderSource));
			}

			if (shaderSource.Contains("TESSELATION_CONTROL"))
			{
				sources.Add(Renderer.ShaderType.TessControlShader, InsertHeader("TESSELATION_CONTROL", defines, shaderSource));
			}

			if (shaderSource.Contains("TESSELATION_EVALUATION"))
			{
				sources.Add(Renderer.ShaderType.TessEvaluationShader, InsertHeader("TESSELATION_EVALUATION", defines, shaderSource));
			}

			if (shaderSource.Contains("COMPUTE"))
			{
				sources.Add(Renderer.ShaderType.ComputeShader, InsertHeader("COMPUTE", defines, shaderSource));
			}

			foreach (var source in sources)
			{
				var type = source.Key.ToString().Substring(0, 4).ToLowerInvariant();
				OutputShader(shader.Name, type, parameters, source.Value);
			}

            shader.Parameters = parameters;

			if (shader.Handle == -1)
				shader.Handle = Backend.RenderSystem.CreateShader();

			string errors;
			var success = Backend.RenderSystem.SetShaderData(shader.Handle, sources, out errors);

			if (success)
			{
				shader.Uniforms = Backend.RenderSystem.GetUniforms(shader.Handle);
			}

			if (!string.IsNullOrWhiteSpace(errors))
				Common.Log.WriteLine(shader.Name + ": " + errors, success ? Common.LogLevel.Default : Common.LogLevel.Error);
		}

		public void Unload(object resource)
		{
			var shader = (ShaderProgram)resource;
			Backend.RenderSystem.DestroyShader(shader.Handle);
			shader.Handle = -1;
		}
	}
}
