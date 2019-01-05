using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Logging;
using Triton.Resources;
using Triton.Utility;

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
    class ShaderSerializer : Triton.Resources.IResourceSerializer<ShaderProgram>
    {
        private readonly Backend _backend;
        private readonly Triton.IO.FileSystem _fileSystem;
        private readonly Dictionary<string, List<ShaderProgram>> _shaders = new Dictionary<string, List<ShaderProgram>>();
        private readonly Dictionary<string, HashSet<string>> _dependencies = new Dictionary<string, HashSet<string>>();
        private readonly ResourceManager _resourceManager;

        public bool SupportsStreaming => false;

        public ShaderSerializer(Backend backend, Triton.IO.FileSystem fileSystem, ResourceManager resourceManager)
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
            return string.Format("#version 460 core\n#define {0}\n{1}\n", type, defines) + source;
        }

        private Dictionary<Renderer.ShaderType, string> GetShaderSources(string name, string shaderSource, string parameters)
        {
            var preProcessor = new Shaders.Preprocessor(_fileSystem);

            shaderSource = preProcessor.Process(shaderSource);
            foreach (var dependency in preProcessor.Dependencies)
            {
                if (!_dependencies.ContainsKey(dependency))
                {
                    _dependencies.Add(dependency, new HashSet<string>());
                }

                _dependencies[dependency].Add(name);
            }

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

            return sources;
        }

        private void LoadInternal(ShaderProgram shader, string name, string shaderSource, string parameters)
        {
            var sources = GetShaderSources(name, shaderSource, parameters);

            var temporaryShaderHandle = _backend.RenderSystem.CreateShader();
            var success = _backend.RenderSystem.SetShaderData(temporaryShaderHandle, sources, out var errors);

            if (success)
            {
                // Destroy existing if it's a reload of the  shader
                if (shader.Handle >= 0)
                {
                    shader.Reset();
                    _backend.RenderSystem.DestroyShader(shader.Handle);
                }

                shader.Handle = temporaryShaderHandle;
                shader.Uniforms = _backend.RenderSystem.GetUniforms(shader.Handle);
            }

            if (!string.IsNullOrWhiteSpace(errors))
                Log.WriteLine(name + ": " + errors, success ? LogLevel.Info : LogLevel.Error);
        }

        public Task Deserialize(object resource, byte[] data)
        {
            var shader = (ShaderProgram)resource;
            var (name, parameters) = _resourceManager.GetResourceProperties(shader);
            var shaderSource = Encoding.ASCII.GetString(data); // Complete source of both shaders before splitting them

            LoadInternal(shader, name, shaderSource, parameters);

            // The same shader can exist with different parameters
            if (!_shaders.ContainsKey(name))
            {
                _shaders.Add(name, new List<ShaderProgram>());
            }

            _shaders[name].Add(shader);

            return Task.FromResult(0);
        }

        private void ReloadShader(string name)
        {
            if (!_shaders.ContainsKey(name))
            {
                Log.WriteLine($"Untracked shader '{name}'", LogLevel.Warning);
                return;
            }

            var path = name + ".glsl";
            string shaderSource;

            using (var stream = _fileSystem.OpenRead(path))
            using (var reader = new System.IO.StreamReader(stream))
            {
                shaderSource = reader.ReadToEnd();
            }

            foreach (var shader in _shaders[name])
            {
                var (_, parameters) = _resourceManager.GetResourceProperties(shader);
                LoadInternal(shader, name, shaderSource, parameters);
            }
        }

        public void Reload(string path)
        {
            var name = path.Replace(".glsl", "");

            // Check if it's an indiret or a direct change
            if (_dependencies.ContainsKey(path))
            {
                foreach (var shader in _dependencies[path])
                {
                    ReloadShader(shader);
                }
            }
            else if (_shaders.ContainsKey(name))
            {
                ReloadShader(name);
            }
        }

        public byte[] Serialize(object resource)
            => throw new NotImplementedException();
    }

    class ShaderHotReloader
    {
        private readonly System.IO.FileSystemWatcher _watcher;
        private readonly HashSet<string> _changedFiles = new HashSet<string>();

        private ShaderSerializer _loader;
        private readonly string _path;
        private readonly string _basePath;

        public ShaderHotReloader(ShaderSerializer loader, string path, string basePath)
        {
            _loader = loader;
            _path = path;
            _basePath = basePath;

            _watcher = new System.IO.FileSystemWatcher(path)
            {
                NotifyFilter = System.IO.NotifyFilters.LastWrite
            };

            _watcher.Changed += FileChanged;
            _watcher.EnableRaisingEvents = true;
            _watcher.IncludeSubdirectories = true;
        }

        public void Tick()
        {
            lock (_changedFiles)
            {
                foreach (var file in _changedFiles)
                {
                    _loader.Reload(file);
                }
                _changedFiles.Clear();
            }
        }

        private void FileChanged(object sender, System.IO.FileSystemEventArgs e)
        {
            lock (_changedFiles)
            {
                var path = _basePath + e.FullPath.Replace(_path, "").Replace('\\', '/');
                _changedFiles.Add(path);
            }
        }
    }

    public class ShaderHotReloadConfig
    {
        public string Path { get; set; }
        public string BasePath { get; set; }
        public bool Enable { get; set; }
    }
}
