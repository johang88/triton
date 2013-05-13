using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Triton.Graphics.Shaders
{
	class Preprocessor
	{
		private readonly Triton.Common.IO.FileSystem FileSystem;
		private static Regex PreprocessorRegex = new Regex(@"^(attrib|uniform|sampler)\(([ \t\w]*)(,[ \t\w]*)+\)$", RegexOptions.Multiline);

		public Preprocessor(Triton.Common.IO.FileSystem fileSystem)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			FileSystem = fileSystem;
		}

		public string Process(string source)
		{
			return source;
		}
	}
}
