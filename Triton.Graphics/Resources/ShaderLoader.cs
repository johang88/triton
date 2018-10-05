using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;

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
        private readonly Backend _backend;
        private readonly Triton.Common.IO.FileSystem _fileSystem;
        private readonly Dictionary<string, ShaderProgram> _shaders = new Dictionary<string, ShaderProgram>();
        private object _reloadLock = new object();
        private readonly ResourceManager _resourceManager;

        public bool SupportsStreaming => false;

        public ShaderLoader(Backend backend, Triton.Common.IO.FileSystem fileSystem, ResourceManager resourceManager)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        }

        public string Extension { get { return ".glsl"; } }
        public string DefaultFilename { get { return ""; } }

        private void OutputShader(string name, string type, string parameters, string content)
        {
            if (!string.IsNullOrWhiteSpace(content) && _fileSystem.DirectoryExists("/tmp/"))
            {
                parameters = parameters.Replace(',', '_').Replace('/', '_').Replace('\\', '_');
                if (!string.IsNullOrWhiteSpace(parameters))
                    parameters = '-' + parameters;
                var filename = name.Replace('/', '_') + parameters + '.' + type;
                using (var stream = _fileSystem.OpenWrite("/tmp/" + filename))
                using (var writer = new System.IO.StreamWriter(stream))
                {
                    writer.Write(content);
                }
            }
        }

        public object Create(Type type)
            => new ShaderProgram(_backend);

        private string InsertHeader(string type, string defines, string source)
        {
            return string.Format("#version 410 core\n#define {0}\n{1}\n", type, defines) + source;
        }

        public Task Load(object resource, byte[] data)
        {
            var shader = (ShaderProgram)resource;
            var (name, parameters) = _resourceManager.GetResourceProperties(shader);

            // This will reset some cache data like uniform locations
            shader.Reset();

            var filename = name + ".glsl";

            var shaderSource = Encoding.ASCII.GetString(data); // Complete source of both shaders before splitting them

            var preProcessor = new Shaders.Preprocessor(_fileSystem);

            shaderSource = preProcessor.Process(shaderSource);

            var defines = "";

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
                OutputShader(name, type, parameters, source.Value);
            }

            if (shader.Handle == -1)
                shader.Handle = _backend.RenderSystem.CreateShader();

            var success = _backend.RenderSystem.SetShaderData(shader.Handle, sources, out var errors);

            if (success)
            {
                shader.Uniforms = _backend.RenderSystem.GetUniforms(shader.Handle);
            }

            if (!string.IsNullOrWhiteSpace(errors))
                Common.Log.WriteLine(name + ": " + errors, success ? Common.LogLevel.Default : Common.LogLevel.Error);

            return Task.FromResult(0);
        }

        public void Unload(object resource)
        {
            var shader = (ShaderProgram)resource;
            _backend.RenderSystem.DestroyShader(shader.Handle);
            shader.Handle = -1;
        }
    }
}
