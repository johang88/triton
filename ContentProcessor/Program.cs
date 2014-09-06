using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Triton.Common;
using Triton.Content;
using Triton.Content.Database;
using ServiceStack.Text;
using ServiceStack.Text.Jsv;

namespace ContentProcessor
{
	class Program
	{
		private const string ContentDBFilename = "content.db";
		private const string MeshConverterPath = "MeshConverter.exe";

		private readonly Triton.Common.CommandLineApplication Application;

		private readonly Factory<string, Triton.Content.ICompiler> Compilers = new Factory<string, Triton.Content.ICompiler>();
		private Triton.Content.Database.DB Database;

		public Program(string[] parameters)
		{
			Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.Console());
			Log.AddOutputHandler(new Triton.Common.LogOutputHandlers.File("Logs/ContentProcessor.txt"));

			ServiceStack.Text.JsConfig.IncludeTypeInfo = true;

			var extensionToType = new Dictionary<string, string>();
			extensionToType.Add(".mesh", "mesh");
			extensionToType.Add(".mesh.xml", "mesh");
			extensionToType.Add(".dae", "mesh");
			extensionToType.Add(".fbx", "mesh");
			extensionToType.Add(".skeleton", "skeleton");
			extensionToType.Add(".skeleton.xml", "skeleton");
			extensionToType.Add(".png", "texture");
			extensionToType.Add(".tga", "texture");
			extensionToType.Add(".bmp", "texture");
			extensionToType.Add(".jpg", "texture");
			extensionToType.Add(".dds", "texture");
			extensionToType.Add(".mat", "material");

			Compilers.Add("mesh", () => new Triton.Content.Compilers.MeshCompiler());
			Compilers.Add("skeleton", () => new Triton.Content.Compilers.SkeletonCompiler());
			Compilers.Add("texture", () => new Triton.Content.Compilers.TextureCompiler());
			Compilers.Add("material", () => new Triton.Content.Compilers.MaterialCompiler());

			Application = new Triton.Common.CommandLineApplication(parameters, "ContentProcessor in=<input_dir> out=<output_dir>");

			string inputDir = "", outputDir = "";
			bool noCache = false;

			Application.AddCommand("in", "Input dir path", true, "", v => inputDir = v)
			           .AddCommand("out", "Output dir path", true, "", v => outputDir = v)
			           .AddCommand("nocache", "Force recompilation of all resources", false, false, v => noCache = v);

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

			Database = new Triton.Content.Database.DB(Path.Combine(inputDir, ContentDBFilename));
			var context = new CompilationContext(inputDir, outputDir);

			// Import source content to database
			foreach (var file in Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories))
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

				ContentEntry content;
				if (!Database.SourceExists(contentName) && extensionToType.ContainsKey(extension))
				{
					content = new ContentEntry();
					content.Id = fileWithoutExtension.Replace(inputDir, "").Substring(1).Replace('\\', '/');
					content.Source = contentName;
					content.LastCompilation = DateTime.MinValue;
					content.Type = extensionToType[extension]; // todo ... 

					Database.AddEntry(content);
				}
			}

			var entries = Database.GetAllEntries();
			foreach (var entry in entries)
			{
				var sourcePath = Path.Combine(inputDir, entry.Source);
				var outputPath = Path.Combine(outputDir, entry.Id);

				if (!File.Exists(sourcePath))
					continue;

				if (!noCache && File.GetLastWriteTime(entry.Source) <= entry.LastCompilation)
					continue;

				if (Compilers.Exists(entry.Type))
				{
					var fileOutputDirectory = Path.GetDirectoryName(outputPath);
					if (!Directory.Exists(fileOutputDirectory))
					{
						Directory.CreateDirectory(fileOutputDirectory);
					}

					var compiler = Compilers.Create(entry.Type);
					compiler.Compile(context, sourcePath, outputPath, entry);

					Log.WriteLine("Processed {0}", entry.Id);
					entry.LastCompilation = DateTime.Now;

					Database.SaveEntry(entry);
				}
			}
		}

		static void Main(string[] args)
		{
			var program = new Program(args);
		}
	}
}
