using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace Triton.Content.Shaders
{
	class ImportsResolver
	{
		private static Regex PreprocessorImportRegex = new Regex(@"^import\(([ \t\w /]+)\);", RegexOptions.Multiline);
		private readonly Stack<string> BasePath = new Stack<string>();

		public ImportsResolver(string basePath)
		{
			BasePath.Push(basePath);
		}

		public string Process(string source)
		{
			var output = PreprocessorImportRegex.Replace(source, PreprocessorImportReplacer);

			return output;
		}

		string PreprocessorImportReplacer(Match match)
		{
			var relativePath = match.Groups[1].Value + ".glsl";
			var absolutePath = System.IO.Path.Combine(BasePath.Peek(), relativePath);
			var newBasePath = Path.GetDirectoryName(absolutePath);

			BasePath.Push(newBasePath);
			var importedData = Process(File.ReadAllText(absolutePath));
			BasePath.Pop();

			return importedData;
		}
	}
}
