using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Triton.Graphics.Shaders
{
    /// <summary>
    /// Processes glsl shaders, just #include for now
    /// </summary>
    class Preprocessor
    {
        private readonly Triton.Common.IO.FileSystem _fileSystem;
        private static readonly Regex _preprocessorIncludeRegex = new Regex(@"^#include\s""([ \t\w /]+)""", RegexOptions.Multiline);

        public Preprocessor(Triton.Common.IO.FileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public string Process(string source)
        {
            var output = _preprocessorIncludeRegex.Replace(source, PreprocessorImportReplacer);

            return output;
        }

        string PreprocessorImportReplacer(Match match)
        {
            var path = match.Groups[1].Value + ".glsl";
            using (var stream = _fileSystem.OpenRead(path))
            using (var reader = new System.IO.StreamReader(stream))
            {
                return Process(reader.ReadToEnd());
            }
        }
    }
}
