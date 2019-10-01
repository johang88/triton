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
	public class TextureCompiler : ICompiler
	{
        public string Extension => ".dds";
        public int Version => 1;

        public void Compile(CompilationContext context)
		{
			var extension = Path.GetExtension(context.InputPath);

			if (extension != ".dds")
			{
                using (var image = new MagickImage(context.InputPath))
                {
                    image.Format = MagickFormat.Dds;

                    var defines = new DdsWriteDefines
                    {
                        WeightByAlpha = image.HasAlpha,
                        Mipmaps = 1 + (int)System.Math.Floor(System.Math.Log(System.Math.Max(image.Width, image.Height), 2))
                    };

                    // Has to set custom value as DdsCompression enum does not contain dxt5
                    image.Settings.SetDefine(MagickFormat.Dds, "compression", image.HasAlpha ? "dxt5" : "dxt1");

                    image.Write(context.OutputPath, defines);
                }
            }
			else
			{
				File.Copy(context.InputPath, context.OutputPath, true);
			}
		}
	}
}
