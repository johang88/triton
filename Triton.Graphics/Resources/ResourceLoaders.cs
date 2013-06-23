using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public static class ResourceLoaders
	{
		public static void Init(Triton.Common.ResourceManager resourceManager, Backend backend, Triton.Common.IO.FileSystem fileSystem)
		{
			resourceManager.AddResourceLoader<Texture>(new TextureLoader(backend, fileSystem));
			resourceManager.AddResourceLoader<ShaderProgram>(new ShaderLoader(backend, fileSystem));
			resourceManager.AddResourceLoader<Mesh>(new MeshLoader(backend, fileSystem));
			resourceManager.AddResourceLoader<SkeletalAnimation.Skeleton>(new SkeletonLoader(fileSystem));
		}
	}
}
