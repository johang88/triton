using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Triton.Graphics.Shaders
{
	/// <summary>
	/// Processes glsl shaders
	/// 
	/// Handles the following preprocessor commands
	/// * import(absolute_path_without_extension) 
	///		- Includes a glsl file into the currently processed file, included files are not expanded by the preprocessor
	///		- Example: import(shaders/lighting/cook_torrance) => all code in cook_torrance.glsl
	///	* attrib(type, name, externalType)
	///		- Declares an attrib (in) variable, the attrib will be bound to a attrib location determined by externalType
	///		- type is a glsl type, ie vec3, mat4x4
	///		- externalType is any of Position, Normal, Tangent, TexCoord
	///		- Example: attrib(vec3, iPosition, Position) => in vec3 iPosition;
	///	 * uniform(type, name, externalName)
	///		- Declares a uniform and exposes it to the code, might be automatically bound depending on the external name.
	///		  Any automaitc binding depends on the implementing application.
	///		- Example: uniform(vec3, lightColor, LightColor) => uniform vec3 lightColor
	/// </summary>
	class Preprocessor
	{
		private readonly Triton.Common.IO.FileSystem FileSystem;
		private static Regex PreprocessorRegex = new Regex(@"^(attrib|uniform|sampler)\(([ \t\w]*),([ \t\w]*),([ \t\w]*)\);", RegexOptions.Multiline);
		private static Regex PreprocessorImportRegex = new Regex(@"^import\(([ \t\w /]+)\);", RegexOptions.Multiline);

		private List<Attrib> Attribs;
		private List<Uniform> Uniforms;

		public Preprocessor(Triton.Common.IO.FileSystem fileSystem)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			FileSystem = fileSystem;
		}

		public string Process(string source, out Attrib[] attribs, out Uniform[] uniforms)
		{
			Attribs = new List<Attrib>();
			Uniforms = new List<Uniform>();

			var output = PreprocessorImportRegex.Replace(source, PreprocessorImportReplacer);
			output = PreprocessorRegex.Replace(output, PreprocessorReplacer);

			attribs = Attribs.ToArray();
			uniforms = Uniforms.ToArray();

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
				Uniforms.Add(new Uniform
				{
					Name = match.Groups[3].Value,
					BindName = match.Groups[4].Value
				});
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
