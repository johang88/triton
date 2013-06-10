using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace ContentProcessor
{
	class Program
	{
		private const string CacheFilename = "___cache.dat";
		private const string MeshConverterPath = "MeshConverter.exe";

		private readonly Dictionary<string, long> Cache = new Dictionary<string, long>();
		private readonly ManualResetEvent MeshProcessDone = new ManualResetEvent(false);

		private readonly Triton.Common.CommandLineParser CommandLine;

		public Program(string[] parameters)
		{
			CommandLine = new Triton.Common.CommandLineParser(parameters);

			var inputDir = CommandLine.Get<string>("in");
			var outputDir = CommandLine.Get<string>("out");

			if (CommandLine.IsSet("help"))
			{
				PrintUsage();
				return;
			}

			if (string.IsNullOrWhiteSpace(inputDir) || string.IsNullOrWhiteSpace(outputDir))
			{
				PrintUsage();
				return;
			}

			if (!Directory.Exists(inputDir))
			{
				Console.WriteLine("ERROR: Input dir does not exist");
				return;
			}

			if (!Directory.Exists(outputDir))
			{
				Console.WriteLine("Output dir does not exist, creating");
				Directory.CreateDirectory(outputDir);
			}

			if (!CommandLine.IsSet("nocache"))
			{
				LoadCache();
			}

			var searchPattern = CommandLine.Get<string>("pattern", "*");

			foreach (var file in Directory.GetFiles(inputDir, searchPattern, SearchOption.AllDirectories))
			{
				if (Cache.ContainsKey(file) && (File.GetLastWriteTime(file).Ticks - Cache[file]) <= 0)
					continue;

				if (Cache.ContainsKey(file))
					Cache[file] = File.GetLastWriteTime(file).Ticks;
				else
					Cache.Add(file, File.GetLastWriteTime(file).Ticks);

				var fileOutputPath = Path.Combine(outputDir, file.Replace(inputDir, "").Substring(1));

				switch (Path.GetExtension(file).ToLowerInvariant())
				{
					case ".xml":
						if (file.EndsWith(".mesh.xml"))
						{
							ProcessMesh(file, fileOutputPath);
						}
						break;
					case ".png":
						ProcessTexture(file, fileOutputPath);
						break;
				}
			}

			WriteCache();
		}

		void ProcessMesh(string inputFile, string fileOutputPath)
		{
			fileOutputPath = fileOutputPath.Replace(".mesh.xml", ".xml");
			fileOutputPath = Path.ChangeExtension(fileOutputPath, "mesh");
			var process = new System.Diagnostics.Process();
			process.StartInfo.FileName = MeshConverterPath;
			process.StartInfo.Arguments =  string.Format("{0} out={1}", inputFile, fileOutputPath);
			process.StartInfo.UseShellExecute = false;
			process.EnableRaisingEvents = true;
			process.Exited += (s, e) =>
			{
				MeshProcessDone.Set();
			};
			process.Start();

			WaitHandle.WaitAll(new WaitHandle[] { MeshProcessDone });
		}


		void ProcessTexture(string inputFile, string fileOutputPath)
		{
			fileOutputPath = Path.ChangeExtension(fileOutputPath, "texture");
			File.Copy(inputFile, fileOutputPath, true);
		}

		void LoadCache()
		{
			if (!File.Exists(CacheFilename))
				return;

			using (var stream = File.Open(CacheFilename, FileMode.Open, FileAccess.Read, FileShare.Delete))
			using (var reader = new StreamReader(stream))
			{
				while (!reader.EndOfStream)
				{
					var cacheLine = reader.ReadLine().Split('|');
					if (cacheLine.Length != 2)
						continue;

					Cache.Add(cacheLine[0], long.Parse(cacheLine[1]));
				}
			}

			Console.WriteLine("Cache loaded");
		}

		void WriteCache()
		{
			using (var stream = File.Open(CacheFilename, FileMode.Create, FileAccess.Write, FileShare.Delete))
			using (var writer = new StreamWriter(stream))
			{
				foreach (var entry in Cache)
				{
					writer.WriteLine("{0}|{1}", entry.Key, entry.Value);
				}
			}
		}

		void PrintUsage()
		{
			Console.WriteLine("Usage: ContentProcessor in=<input_dir> out=<output_dir>");
			Console.WriteLine("Available commands");
			Console.WriteLine("\thelp: show this text");
			Console.WriteLine("\tpattern=<*>: only files matching this pattern will be included");
			Console.WriteLine("\tnocache: process all files even if it is not neccecary");
		}

		static void Main(string[] args)
		{
			var program = new Program(args);
		}
	}
}
