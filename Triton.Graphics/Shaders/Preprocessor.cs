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
		private static Regex PreprocessorImportRegex = new Regex(@"^imprt\(([ \t\w /]+)\);", RegexOptions.Multiline);

		private List<Attrib> Attribs;

		public Preprocessor(Triton.Common.IO.FileSystem fileSystem)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			FileSystem = fileSystem;
		}

		public string Process(string source, out Attrib[] attribs)
		{
			Attribs = new List<Attrib>();

			var output = PreprocessorImportRegex.Replace(source, PreprocessorImportReplacer);
			output = PreprocessorRegex.Replace(output, PreprocessorReplacer);

			attribs = Attribs.ToArray();

			return output;
		}

		string PreprocessorImportReplacer(Match match)
		{
			var path = match.Groups[1].Value + ".glsl";
			using (var stream = FileSystem.OpenRead(path))
			using (var reader = new System.IO.StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}

		string PreprocessorReplacer(Match match)
		{
			var verb = match.Groups[1].Value;

			if (verb == "attrib")
			{
				var type = (AttribType)Enum.Parse(typeof(AttribType), match.Groups[4].Value, true);
				Attribs.Add(new Attrib
				{
					Name = match.Groups[2].Value,
					Type = type
				});

				return string.Format("in {0} {1};", match.Groups[2].Value, match.Groups[3].Value);
			}
			else if (verb == "uniform")
			{
				return string.Format("uniform {0} {1};", match.Groups[2].Value, match.Groups[3].Value);
			}
			else if (verb == "sampler")
			{
				return string.Format("uniform sampler{0} {1};", match.Groups[2].Value, match.Groups[3].Value);
			}

			return "";
		}
	}
}
