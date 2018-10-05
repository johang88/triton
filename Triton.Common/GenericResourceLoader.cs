using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
    public class GenericResourceLoader : IResourceLoader
    {
        public string Extension => ".v";
        public string DefaultFilename => "missing.v";
        public bool SupportsStreaming => false;

        public object DataContractJsonSerializer { get; private set; }

        public object Create(Type type)
            => Activator.CreateInstance(type);

        public Task Load(object resource, byte[] data)
        {
            JsonConvert.PopulateObject(Encoding.UTF8.GetString(data), resource);
            return Task.FromResult(0);
        }

        public void Unload(object resource)
        {
            // NOP
        }
    }
}
