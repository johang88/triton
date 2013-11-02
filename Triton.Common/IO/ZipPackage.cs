using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace Triton.Common.IO
{
	public class ZipPackage : IPackage
	{
		private readonly ZipArchive Archive;

		public ZipPackage(string path)
		{
			Archive = ZipFile.OpenRead(path);
		}

		public System.IO.Stream OpenFile(string filename)
		{
			filename = filename.Replace('\\', '/');
			var entry = Archive.GetEntry(filename);
			return entry.Open();
		}

		public System.IO.Stream OpenWrite(string filename)
		{
			throw new NotImplementedException();
		}

		public bool FileExists(string filename)
		{
			filename = filename.Replace('\\', '/');
			return Archive.GetEntry(filename) != null;
		}

		public IEnumerable<string> GetDirectories(string filename)
		{
			yield break;
		}

		public IEnumerable<string> GetFiles(string filename, string pattern)
		{
			yield break;
		}

		public bool Writeable
		{
			get { return false;  }
		}
	}
}
