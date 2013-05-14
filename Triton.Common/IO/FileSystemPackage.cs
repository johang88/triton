using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Triton.Common.IO
{
	public class FileSystemPackage : IPackage
	{
		private readonly string Path;
		public bool Writeable { get { return true; } }

		public FileSystemPackage(string path)
		{
			if (!path.EndsWith("\\"))
			{
				path = path + "\\";
			}

			if (!Directory.Exists(path))
			{
				throw new DirectoryNotFoundException(path);
			}

			Path = path;
		}

		public Stream OpenFile(string filename)
		{
			return File.Open(System.IO.Path.Combine(Path, filename), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		}

		public Stream OpenWrite(string filename)
		{
			return File.Open(System.IO.Path.Combine(Path, filename), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
		}

		public bool FileExists(string filename)
		{
			return File.Exists(System.IO.Path.Combine(Path, filename));
		}

		public IEnumerable<string> GetDirectories(string filename)
		{
			foreach (string directory in Directory.GetDirectories(System.IO.Path.Combine(Path, filename)))
			{
				yield return directory.Substring(directory.LastIndexOf('\\') + 1);
			}
		}

		public IEnumerable<string> GetFiles(string filename, string pattern)
		{
			var path = System.IO.Path.Combine(Path, filename);

			if (!Directory.Exists(path))
				yield break;

			foreach (string file in Directory.GetFiles(path, pattern, SearchOption.AllDirectories))
			{
				yield return file.Replace(Path, "");
			}
		}
	}
}
