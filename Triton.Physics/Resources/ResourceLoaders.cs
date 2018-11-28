using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Resources
{
	public static class ResourceLoaders
	{
		public static void Init(Triton.Resources.ResourceManager resourceManager, Triton.IO.FileSystem fileSystem)
		{
			resourceManager.AddResourceSerializer<Mesh>(new MeshLoader(fileSystem));
		}
	}
}
