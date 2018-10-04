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

        public object Create(Type type)
            => Activator.CreateInstance(type);

        public void Load(object resource, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void Unload(object resource)
        {
            throw new NotImplementedException();
        }
    }
}
