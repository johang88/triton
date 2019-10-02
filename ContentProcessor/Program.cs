using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Triton.Content;
using Triton.Tools;
using Triton.Logging;
using Newtonsoft.Json;

namespace ContentProcessor
{
    class Program
    {
        private readonly CommandLineApplication _application;
        private readonly Factory<string, Triton.Content.ICompiler> _compilers = new Factory<string, Triton.Content.ICompiler>();
        private readonly Dictionary<string, string> _extensionToType;

        public Program(string[] parameters)
        {
            Log.AddOutputHandler(new Triton.Logging.Console());
            Log.AddOutputHandler(new Triton.Logging.File("Logs/ContentProcessor.txt"));

            _extensionToType = new Dictionary<string, string>
            {
                { ".mesh", "mesh" },
                { ".x", "mesh" },
                { ".mesh.xml", "mesh" },
                { ".col.xml", "collision" },
                { ".dae", "mesh" },
                { ".fbx", "mesh" },
                { ".skeleton", "skeleton" },
                { ".skeleton.xml", "skeleton" },
                { ".png", "texture" },
                { ".tga", "texture" },
                { ".bmp", "texture" },
                { ".jpg", "texture" },
                { ".dds", "texture" },
                { ".mat", "material" }
            };

            _compilers.Add("mesh", () => new Triton.Content.Compilers.MeshCompiler());
            _compilers.Add("skeleton", () => new Triton.Content.Compilers.SkeletonCompiler());
            _compilers.Add("texture", () => new Triton.Content.Compilers.TextureCompiler());
            _compilers.Add("collision", () => new Triton.Content.Compilers.CollisionMeshCompiler());

            _application = new CommandLineApplication(parameters, "ContentProcessor in=<input_dir> out=<output_dir>");

            string inputDir = "", outputDir = "";
            bool noCache = false;

            _application.AddCommand("in", "Input dir path", true, "", v => inputDir = v)
                       .AddCommand("out", "Output dir path", true, "", v => outputDir = v)
                       .AddCommand("nocache", "Force recompilation of all resources", false, false, v => noCache = v);

            if (!_application.IsValid())
            {
                _application.PrintUsage();
                return;
            }

            if (!Directory.Exists(inputDir))
            {
                Log.WriteLine("ERROR: Input dir does not exist");
                return;
            }

            if (!Directory.Exists(outputDir))
            {
                Log.WriteLine("Output dir does not exist, creating");
                Directory.CreateDirectory(outputDir);
            }

            if (IsPackage(inputDir))
            {
                CompilePackage(inputDir, GetPackageOutputDir(inputDir));
            }
            else
            {
                // Look for packages in root level
                foreach (var dir in Directory.GetDirectories(inputDir).Where(IsPackage))
                {
                    CompilePackage(dir, GetPackageOutputDir(dir));
                }
            }

            bool IsPackage(string dir)
            {
                var path = Path.Combine(dir, "package.json");
                return System.IO.File.Exists(path);
            }

            string GetPackageOutputDir(string dir)
            {
                var name = System.IO.Path.GetFileName(dir);
                return Path.Combine(outputDir, name);
            }
        }

        void CompilePackage(string packagePath, string outputDir)
        {
            var cachePath = Path.Combine(packagePath, "__cache.json");
            var cache = new Dictionary<string, ContentEntry>();

            if (System.IO.File.Exists(cachePath))
            {
                using var stream = System.IO.File.Open(cachePath, FileMode.Open, FileAccess.Read, FileShare.Delete);
                using var reader = new StreamReader(stream);

                cache = JsonConvert.DeserializeObject<Dictionary<string, ContentEntry>>(reader.ReadToEnd());
            }

            // Import missing entries from cache automatically
            foreach (var file in Directory.GetFiles(packagePath, "*", SearchOption.AllDirectories))
            {
                var contentName = file.Replace(packagePath, "").Substring(1).Replace('\\', '/');

                var fileWithoutExtension = Path.GetFileNameWithoutExtension(file);

                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension == ".xml") // Get sub extension
                {
                    extension = Path.GetExtension(Path.GetFileNameWithoutExtension(file)) + ".xml";
                    fileWithoutExtension = Path.GetFileNameWithoutExtension(fileWithoutExtension);
                }

                fileWithoutExtension = Path.Combine(Path.GetDirectoryName(file), fileWithoutExtension);

                if (!cache.ContainsKey(contentName) && _extensionToType.ContainsKey(extension))
                {
                    cache.Add(contentName, new ContentEntry
                    {
                        Id = fileWithoutExtension.Replace(packagePath, "").Substring(1).Replace('\\', '/'),
                        LastCompilation = DateTime.MinValue,
                        Type = _extensionToType[extension] // todo ... 
                    });
                }
            }

            foreach (var entry in cache)
            {
                try
                {
                    if (!_compilers.Exists(entry.Value.Type))
                        continue;

                    var compiler = _compilers.Create(entry.Value.Type);

                    var sourcePath = Path.Combine(packagePath, entry.Key);
                    var outputPath = Path.Combine(outputDir, entry.Value.Id) + compiler.Extension;

                    if (!System.IO.File.Exists(sourcePath))
                        continue;

                    if (entry.Value.Version == compiler.Version && System.IO.File.GetLastWriteTime(sourcePath) <= entry.Value.LastCompilation)
                        continue;

                    var fileOutputDirectory = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(fileOutputDirectory))
                    {
                        Directory.CreateDirectory(fileOutputDirectory);
                    }

                    string metaData = null;
                    var metaDataPath = entry.Key + ".json";
                    if (System.IO.File.Exists(metaDataPath))
                    {
                        metaData = System.IO.File.ReadAllText(metaDataPath);
                    }

                    // string name, string inputPath, string outputPath, string baseOutputPath, string metaData
                    var context = new CompilationContext(
                        sourcePath,
                        outputPath,
                        outputDir,
                        metaData
                        );

                    Log.WriteLine($"Compiling {entry.Value.Id}");
                    compiler.Compile(context);
                    Log.WriteLine($"Compiled {entry.Value.Id}");

                    entry.Value.LastCompilation = DateTime.Now;
                    entry.Value.Version = compiler.Version;
                }
                catch (Exception e)
                {
                    Log.WriteLine(string.Format("{0}\n\t{1}", entry.Key, e.ToString()), LogLevel.Error);
                }
            }

            System.IO.File.WriteAllText(cachePath, JsonConvert.SerializeObject(cache, Formatting.Indented));
        }

        static void Main(string[] args)
        {
            _ = new Program(args);
        }

        public class ContentEntry
        {
            public string Id { get; set; }
            public DateTime LastCompilation { get; set; }
            public string Type { get; set; }
            public int Version { get; set; }
        }
    }
}
