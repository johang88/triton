using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public static class ResourceLoaders
	{
		public static void Init(Triton.Common.ResourceManager resourceManager, Backend backend, Triton.Common.IO.FileSystem fileSystem, ShaderHotReloadConfig shaderHotReloadConfig)
		{
            var shaderLoader = new ShaderLoader(backend, fileSystem, resourceManager);

            resourceManager.AddResourceLoader(new TextureLoader(backend, fileSystem));
			resourceManager.AddResourceLoader(shaderLoader);
			resourceManager.AddResourceLoader(new MeshLoader(backend, resourceManager, fileSystem));
			resourceManager.AddResourceLoader(new SkeletonLoader(fileSystem));
			resourceManager.AddResourceLoader(new MaterialLoader(resourceManager, fileSystem));
			resourceManager.AddResourceLoader(new BitmapFontLoader(resourceManager, fileSystem));

            backend.ConfigureShaderHotReloading(shaderLoader, shaderHotReloadConfig);
		}
	}
}
