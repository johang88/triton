using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Triton.Content.Compilers
{
	public class TextureCompiler : ICompiler
	{
		public void Compile(string inputPath, string outputPath)
		{
			var extension = Path.GetExtension(inputPath);

			if (extension != ".dds")
			{
				outputPath = Path.ChangeExtension(outputPath, "dds");

				var filename = Path.GetFileNameWithoutExtension(inputPath);

				var isNormal = filename.EndsWith("_n");

				var arguments = "";
				if (isNormal)
				{
					arguments += "-normal ";
					arguments += "-bc3 ";
				}
				else
				{
					arguments += "-color ";
					arguments += "-bc3 ";
				}

				arguments += inputPath + " ";
				arguments += outputPath;

				var startInfo = new ProcessStartInfo(@"C:\Program Files\NVIDIA Corporation\NVIDIA Texture Tools 2\bin\nvcompress", arguments);
				startInfo.UseShellExecute = false;
				Process.Start(startInfo);
			}
			else
			{
				File.Copy(inputPath, outputPath, true);
			}
		}
	}
}
