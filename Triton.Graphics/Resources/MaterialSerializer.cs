using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Utility;

namespace Triton.Graphics.Resources
{
	class MaterialSerializer : Triton.Resources.IResourceSerializer<Material>
	{
		private readonly Triton.IO.FileSystem _fileSystem;
		private readonly Triton.Resources.ResourceManager _resourceManager;

        public bool SupportsStreaming => false;

        public MaterialSerializer(Triton.Resources.ResourceManager resourceManager, Triton.IO.FileSystem fileSystem)
		{
            _fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
			_resourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
		}

		public string Extension { get { return ".mat.v"; } }
		public string DefaultFilename { get { return "/materials/default.mat.v"; } }

        public object Create(Type type)
            => new Material(_resourceManager);

		public async Task Deserialize(object resource, byte[] data)
		{
            var material = (Material)resource;
            material.Dispose();

            var (_, parameters) = _resourceManager.GetResourceProperties(material);

            using (var stream = new System.IO.MemoryStream(data))
			using (var reader = new System.IO.StreamReader(stream))
			{
                var materialJsonData = await reader.ReadToEndAsync();
                var materialDesc = JsonConvert.DeserializeObject<MaterialDesc>(materialJsonData);

                var shaderDefines = new List<string>();

                if (materialDesc.Textures != null)
                {
                    foreach (var texture in materialDesc.Textures)
                    {
                        var samplerName = $"sampler{texture.Key}";
                        var define = $"HAS_SAMPLER_{texture.Key.ToUpperInvariant()}";

                        shaderDefines.Add(define);
                        material.Textures.Add(samplerName, await _resourceManager.LoadAsync<Texture>(texture.Value));
                    }
                }

                if (materialDesc.Defines?.Length > 0)
                {
                    shaderDefines.AddRange(materialDesc.Defines);
                }

                if (materialDesc.Uniforms != null)
                {
                    foreach (var uniform in materialDesc.Uniforms)
                    {
                        var uniformName = $"u{uniform.Key}";
                        var define = $"HAS_{uniform.Key.ToUpperInvariant()}";

                        shaderDefines.Add(define);

                        // Derive type
                        if (uniform.Value.Contains(' '))
                        {
                            // Assume vec4 for now
                            material.AddUniform(uniformName, StringConverter.ParseVector4(uniform.Value));
                        }
                        else
                        {
                            // Assume float for now
                            material.AddUniform(uniformName, StringConverter.Parse<float>(uniform.Value));
                        }
                    }
                }

                material.Shader = await _resourceManager.LoadAsync<ShaderProgram>(materialDesc.Shader, string.Join(";", shaderDefines));
            }
		}

        public byte[] Serialize(object resource)
            => throw new NotImplementedException();

        class MaterialDesc
        {
            public string Shader { get; set; }
            public Dictionary<string, string> Textures { get; set; }
            public Dictionary<string, string> Uniforms { get; set; }
            public string[] Defines { get; set; }
        }
    }
}
