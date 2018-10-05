using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Resources
{
	public static class ResourceLoaders
	{
		public static void Init(Triton.Common.ResourceManager resourceManager, Triton.Common.IO.FileSystem fileSystem)
		{
			resourceManager.AddResourceLoader<Mesh>(new MeshLoader(fileSystem));
		}
	}
}
