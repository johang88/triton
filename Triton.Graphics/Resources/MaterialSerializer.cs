using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	class MaterialSerializer : Triton.Common.IResourceSerializer<Material>
	{
        private static readonly char[] Magic = new char[] { 'M', 'A', 'T', 'E' };
		private const int Version_1_0 = 0x01;

		private readonly Triton.Common.IO.FileSystem _fileSystem;
		private readonly Triton.Common.ResourceManager _resourceManager;

		private const string DefaultShader = "/shaders/deferred/gbuffer";

        public bool SupportsStreaming => false;

        public MaterialSerializer(Triton.Common.ResourceManager resourceManager, Triton.Common.IO.FileSystem fileSystem)
		{
            _fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
			_resourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
		}

		public string Extension { get { return ".mat"; } }
		public string DefaultFilename { get { return "/materials/default.mat"; } }

        public object Create(Type type)
            => new Material(_resourceManager);

		public async Task Deserialize(object resource, byte[] data)
		{
            var material = (Material)resource;
            material.Dispose();

            var (_, parameters) = _resourceManager.GetResourceProperties(material);
            material.IsSkinned = parameters.Contains("SKINNED");

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
				material.Shader = await _resourceManager.LoadAsync<ShaderProgram>(shaderPath, material.IsSkinned ? "SKINNED" : "");

				var samplerCount = reader.ReadInt32();
				for (var i = 0; i < samplerCount; i++)
				{
					var samplerName = reader.ReadString();
					var texturePath = reader.ReadString();

					material.Textures.Add(samplerName, await _resourceManager.LoadAsync<Texture>(texturePath));
				}
			}
		}

        public byte[] Serialize(object resource)
            => throw new NotImplementedException();
    }
}
