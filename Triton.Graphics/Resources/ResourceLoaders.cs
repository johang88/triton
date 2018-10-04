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
			resourceManager.AddResourceLoader<ShaderProgram>(new ShaderLoader(backend, fileSystem, resourceManager));
			resourceManager.AddResourceLoader<Mesh>(new MeshLoader(backend, resourceManager, fileSystem));
			resourceManager.AddResourceLoader<SkeletalAnimation.Skeleton>(new SkeletonLoader(fileSystem));
			resourceManager.AddResourceLoader<Material>(new MaterialLoader(resourceManager, fileSystem));
			resourceManager.AddResourceLoader<BitmapFont>(new BitmapFontLoader(resourceManager, fileSystem));
		}
	}
}
