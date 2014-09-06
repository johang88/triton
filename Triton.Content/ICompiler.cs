using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content
{
    public interface ICompiler
    {
		void Compile(CompilationContext context, string inputPath, string outputPath, Database.ContentEntry contentData);
    }
}
