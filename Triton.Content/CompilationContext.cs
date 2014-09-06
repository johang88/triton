﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Triton.Content
{
	public class CompilationContext
	{
		private readonly string InputPath;
		private readonly string OutputPath;
		
		public CompilationContext(string inputPath, string outputPath)
		{
			InputPath = inputPath;
			OutputPath = outputPath;
		}

		public string GetInputPath(string path)
		{
			return Path.Combine(InputPath, path);
		}

		public string GetOutputPath(string path)
		{
			return Path.Combine(OutputPath, path);
		}

		public Stream OpenInput(string path)
		{
			return File.OpenRead(GetInputPath(path));
		}

		public Stream OpenOutput(string path)
		{
			path = GetOutputPath(path);

			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			return File.Open(path, FileMode.Create);
		}
	}
}
