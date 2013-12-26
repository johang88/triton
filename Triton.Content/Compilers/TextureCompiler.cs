using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Triton.Content.Compilers
{
	public class TextureSettings
	{
		public bool IsNormalMap { get; set; }
	}

	public class TextureCompiler : ICompiler
	{
		public void Compile(string inputPath, string outputPath, ContentData contentData)
		{
			var filename = Path.GetFileNameWithoutExtension(inputPath);
			var extension = Path.GetExtension(inputPath);

			outputPath += ".dds";

			TextureSettings settings;

			if (contentData.Settings == null || !(contentData.Settings is TextureSettings))
			{
				settings = new TextureSettings();
				contentData.Settings = settings;

				settings.IsNormalMap = filename.EndsWith("_n");
			}
			else
			{
				settings = contentData.Settings as TextureSettings;
			}

			if (extension != ".dds")
			{
				var arguments = "";
				if (settings.IsNormalMap)
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

				var startInfo = new ProcessStartInfo(@"nvcompress", arguments);
				startInfo.UseShellExecute = false;
				var process = Process.Start(startInfo);
				process.WaitForExit();
			}
			else
			{
				File.Copy(inputPath, outputPath, true);
			}
		}
	}
}
