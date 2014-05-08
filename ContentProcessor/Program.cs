using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Triton.Common;
using Triton.Content;
using ServiceStack.Text;
using ServiceStack.Text.Jsv;

namespace ContentProcessor
{
	class Program
	{
		private const string ContentDBFilename = "content_db.v";
		private const string MeshConverterPath = "MeshConverter.exe";

		private readonly ManualResetEvent MeshProcessDone = new ManualResetEvent(false);

		private readonly Triton.Common.CommandLineApplication Application;

		private readonly Factory<string, Triton.Content.ICompiler> Compilers = new Factory<string, Triton.Content.ICompiler>();
		private Dictionary<string, ContentData> ContentDB = new Dictionary<string, ContentData>();

		public Program(string[] parameters)
		{
			Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.Console());
			Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.File("Logs/ContentProcessor.txt"));

			ServiceStack.Text.JsConfig.IncludeTypeInfo = true;

			Compilers.Add(".mesh", () => new Triton.Content.Compilers.MeshCompiler());
			Compilers.Add(".mesh.xml", () => new Triton.Content.Compilers.MeshCompiler());
			Compilers.Add(".dae", () => new Triton.Content.Compilers.MeshCompiler());
			Compilers.Add(".skeleton", () => new Triton.Content.Compilers.SkeletonCompiler());
			Compilers.Add(".skeleton.xml", () => new Triton.Content.Compilers.SkeletonCompiler());
			Compilers.Add(".png", () => new Triton.Content.Compilers.TextureCompiler());
			Compilers.Add(".tga", () => new Triton.Content.Compilers.TextureCompiler());
			Compilers.Add(".bmp", () => new Triton.Content.Compilers.TextureCompiler());
			Compilers.Add(".jpg", () => new Triton.Content.Compilers.TextureCompiler());
			Compilers.Add(".dds", () => new Triton.Content.Compilers.TextureCompiler());
			Compilers.Add(".shader", () => new Triton.Content.Compilers.ShaderCompiler());

			Application = new Triton.Common.CommandLineApplication(parameters, "ContentProcessor in=<input_dir> out=<output_dir>");

			string inputDir = "", outputDir = "", searchPattern = "";
			bool noCache = false;

			Application.AddCommand("in", "Input dir path", true, "", v => inputDir = v)
			           .AddCommand("out", "Output dir path", true, "", v => outputDir = v)
			           .AddCommand("nocache", "Force recompilation of all resources", false, false, v => noCache = v)
					   .AddCommand("pattern", "Only files matching this pattern will be included", false, "*", v => searchPattern = v);

			if (!Application.IsValid())
			{
				Application.PrintUsage();
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

			var dbPath = Path.Combine(inputDir, ContentDBFilename);
			if (File.Exists(dbPath))
			{
				using (var stream = File.Open(dbPath, FileMode.Open, FileAccess.Read, FileShare.Delete))
				{
					ContentDB = TypeSerializer.DeserializeFromStream<Dictionary<string, ContentData>>(stream);
				}
			}

			foreach (var file in Directory.GetFiles(inputDir, searchPattern, SearchOption.AllDirectories))
			{
				var contentName = file.Replace(inputDir, "").Substring(1).Replace('\\', '/');

				var fileWithoutExtension = Path.GetFileNameWithoutExtension(file);

				var extension = Path.GetExtension(file).ToLowerInvariant();
				if (extension == ".xml") // Get sub extension
				{
					extension = Path.GetExtension(Path.GetFileNameWithoutExtension(file)) + ".xml";
					fileWithoutExtension = Path.GetFileNameWithoutExtension(fileWithoutExtension);
				}

				fileWithoutExtension = Path.Combine(Path.GetDirectoryName(file), fileWithoutExtension);

				ContentData content;
				if (!ContentDB.TryGetValue(contentName, out content))
				{
					content = new ContentData();
					content.OutputPath = fileWithoutExtension.Replace(inputDir, "").Substring(1).Replace('\\', '/');
					ContentDB.Add(contentName, content);
				}

				if (!noCache && File.GetLastWriteTime(file) <= content.LastCompilation)
					continue;

				var outputPath = Path.Combine(outputDir, content.OutputPath);

				if (Compilers.Exists(extension))
				{
					var fileOutputDirectory = Path.GetDirectoryName(outputPath);
					if (!Directory.Exists(fileOutputDirectory))
					{
						Directory.CreateDirectory(fileOutputDirectory);
					}

					var compiler = Compilers.Create(extension);
					compiler.Compile(file, outputPath, content);

					Log.WriteLine("Processed {0}", file);
					content.LastCompilation = DateTime.Now;
				}
			}

			File.WriteAllText(dbPath, ContentDB.ToJsv());
		}

		static void Main(string[] args)
		{
			var program = new Program(args);
		}
	}
}
