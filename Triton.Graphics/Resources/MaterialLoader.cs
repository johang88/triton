using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	class MaterialLoader : Triton.Common.IResourceLoader<Material>
	{
		private readonly Triton.Common.IO.FileSystem FileSystem;
		private readonly Triton.Common.ResourceManager ResourceManager;

		public MaterialLoader(Triton.Common.ResourceManager resourceManager, Triton.Common.IO.FileSystem fileSystem)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			FileSystem = fileSystem;
			ResourceManager = resourceManager;
		}

		public Common.Resource Create(string name, string parameters)
		{
			return new Material(name, parameters);
		}

		public void Load(Common.Resource resource, string parameters, Action<Common.Resource> onLoaded)
		{
			var material = (Material)resource;
			var filename = resource.Name + ".mat.v";

			using (var stream = FileSystem.OpenRead(filename))
			{
				var materialDefinition = ServiceStack.Text.JsonSerializer.DeserializeFromStream<MaterialDefintion>(stream);

				if (!string.IsNullOrWhiteSpace(materialDefinition.diffuse))
					material.Diffuse = ResourceManager.Load<Texture>(materialDefinition.diffuse, "srgb");
				if (!string.IsNullOrWhiteSpace(materialDefinition.normal))
					material.Normal = ResourceManager.Load<Texture>(materialDefinition.normal);
				if (!string.IsNullOrWhiteSpace(materialDefinition.gloss))
					material.Gloss = ResourceManager.Load<Texture>(materialDefinition.gloss);
				if (!string.IsNullOrWhiteSpace(materialDefinition.specular))
					material.Specular = ResourceManager.Load<Texture>(materialDefinition.specular);
			}

			onLoaded(material);
		}

		public void Unload(Common.Resource resource)
		{
			var material = (Material)resource;
			material.Diffuse = null;
			material.Normal = null;
			material.Gloss = null;
			material.Specular = null;
		}

		class MaterialDefintion
		{
			public string diffuse { get; set; }
			public string normal { get; set; }
			public string gloss { get; set; }
			public string specular { get; set; }
		}
	}
}
