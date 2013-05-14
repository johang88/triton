using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Triton.Common.IO
{
	public interface IPackage
	{
		Stream OpenFile(string filename);
		Stream OpenWrite(string filename);
		bool FileExists(string filename);
		IEnumerable<string> GetDirectories(string filename);
		IEnumerable<string> GetFiles(string filename, string pattern);
		bool Writeable { get; }
	}
}
