using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using ImageMagick;
using ImageMagick.Defines;

namespace Triton.Content.Compilers
{
	public class TextureSettings
	{
		public bool IsNormalMap { get; set; }
	}

	public class TextureCompiler : ICompiler
	{
		public void Compile(CompilationContext context, string inputPath, string outputPath, Database.ContentEntry contentData)
		{
			var filename = Path.GetFileNameWithoutExtension(inputPath);
			var extension = Path.GetExtension(inputPath);

			outputPath += ".dds";

			var settings = new TextureSettings();
			settings.IsNormalMap = filename.EndsWith("_n");

			if (extension != ".dds")
			{
                using (var image = new MagickImage(inputPath))
                {
                    image.Format = MagickFormat.Dds;

                    var defines = new DdsWriteDefines
                    {
                        Compression = image.HasAlpha ? DdsCompression.None : DdsCompression.Dxt1, // Wat, why no dxt5???
                        WeightByAlpha = image.HasAlpha,
                        Mipmaps = 1 + (int)System.Math.Floor(System.Math.Log(System.Math.Max(image.Width, image.Height), 2))
                    };

                    image.Write(outputPath, defines);
                }
            }
			else
			{
				File.Copy(inputPath, outputPath, true);
			}
		}
	}
}
