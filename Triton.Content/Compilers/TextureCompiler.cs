using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Triton.Content.Compilers
{
	public class TextureCompiler : ICompiler
	{
		public void Compile(string inputPath, string outputPath)
		{
			outputPath = Path.ChangeExtension(outputPath, "texture");
			File.Copy(inputPath, outputPath, true);
		}
	}
}
