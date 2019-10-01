using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Triton.Content
{
    public class CompilationContext
    {
        public string InputPath { get; }
        public string OutputPath { get; }

        private readonly string _metaData;
        private readonly string _baseOutputPath;

        public CompilationContext(string inputPath, string outputPath, string baseOutputPath, string metaData)
        {
            InputPath = inputPath ?? throw new ArgumentNullException(nameof(inputPath));
            OutputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
            _metaData = metaData;
            _baseOutputPath = baseOutputPath ?? throw new ArgumentNullException(nameof(baseOutputPath));
        }

        public string GetReferencePath(string outputPath)
            => outputPath.Replace(_baseOutputPath, "").Replace("\\", "/");

        public TMetaData GetMetaData<TMetaData>()
            => _metaData != null ? JsonConvert.DeserializeObject<TMetaData>(_metaData) : default;
    }
}
