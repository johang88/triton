using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	class MaterialLoader : Triton.Common.IResourceLoader<Material>
	{
		static readonly char[] Magic = new char[] { 'M', 'A', 'T', 'E' };
		const int Version_1_0 = 0x01;

		private readonly Triton.Common.IO.FileSystem FileSystem;
		private readonly Triton.Common.ResourceManager ResourceManager;

		private const string DefaultShader = "/shaders/deferred/gbuffer";

		public MaterialLoader(Triton.Common.ResourceManager resourceManager, Triton.Common.IO.FileSystem fileSystem)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			FileSystem = fileSystem;
			ResourceManager = resourceManager;
		}

		public string Extension { get { return ".mat"; } }

		public Common.Resource Create(string name, string parameters)
		{
			return new Material(name, parameters, ResourceManager, false);
		}

		public void Load(Common.Resource resource, byte[] data)
		{
			Unload(resource);

			var material = (Material)resource;

			using (var stream = new System.IO.MemoryStream(data))
			using (var reader = new System.IO.BinaryReader(stream))
			{
				var magic = reader.ReadChars(4);
				for (var i = 0; i < Magic.Length; i++)
				{
					if (magic[i] != Magic[i])
						throw new ArgumentException("invalid material");
				}

				var version = reader.ReadInt32();

				var validVersions = new int[] { Version_1_0 };

				if (!validVersions.Contains(version))
					throw new ArgumentException("invalid material, unknown version");

				var shaderPath = reader.ReadString();
				material.Shader = ResourceManager.Load<ShaderProgram>(shaderPath);

				var samplerCount = reader.ReadInt32();
				for (var i = 0; i < samplerCount; i++)
				{
					var samplerName = reader.ReadString();
					var texturePath = reader.ReadString();

					material.Textures.Add(samplerName, ResourceManager.Load<Texture>(texturePath));
				}
			}
		}

		public void Unload(Common.Resource resource)
		{
			var material = (Material)resource;
			material.Unload();
		}
	}
}
