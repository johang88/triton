using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content
{
    public interface ICompiler
    {
		void Compile(string inputPath, string outputPath);
    }
}
