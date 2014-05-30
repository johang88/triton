using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace Triton.Graphics.Resources
{
	class MaterialLoader : Triton.Common.IResourceLoader<Material>
	{
		private readonly Triton.Common.IO.FileSystem FileSystem;
		private readonly Triton.Common.ResourceManager ResourceManager;

		private const string DefaultShader = "/shaders/deferred/gbuffer";
		private const string DefaultSplatShader = "/shaders/deferred/gbuffer_splat";

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
			var filename = name + ".mat.v";

			using (var stream = FileSystem.OpenRead(filename))
			using(var reader = new System.IO.StreamReader(stream))
			{
				var definition = JsonObject.Parse(reader.ReadToEnd());
				var type = definition.Get("type") ?? "standard";

				if (type == "splat")
				{
					Material material = new Materials.SplatMaterial(name, parameters, ResourceManager);
					material.Definition = definition;

					return material;
				}
				else
				{
					var isSkinned = Common.StringConverter.ParseBool(definition.Get("is_skinned") ?? "false");

					Material material = new Materials.StandardMaterial(name, parameters, ResourceManager, isSkinned);
					material.Definition = definition;

					return material;
				}
			}
		}

		public void Load(Common.Resource resource, string parameters, Action<Common.Resource> onLoaded)
		{
			if (resource is Materials.SplatMaterial)
			{
				var material = (Materials.SplatMaterial)resource;
				var filename = resource.Name + ".mat.v";

				var definition = material.Definition;

				if (!string.IsNullOrWhiteSpace(definition.Get("diffuse1")))
					material.Diffuse1 = ResourceManager.Load<Texture>(definition.Get("diffuse1"), "srgb");
				if (!string.IsNullOrWhiteSpace(definition.Get("diffuse2")))
					material.Diffuse2 = ResourceManager.Load<Texture>(definition.Get("diffuse2"), "srgb");
				if (!string.IsNullOrWhiteSpace(definition.Get("diffuse3")))
					material.Diffuse3 = ResourceManager.Load<Texture>(definition.Get("diffuse3"), "srgb");
				if (!string.IsNullOrWhiteSpace(definition.Get("diffuse4")))
					material.Diffuse4 = ResourceManager.Load<Texture>(definition.Get("diffuse4"), "srgb");
				if (!string.IsNullOrWhiteSpace(definition.Get("normal1")))
					material.Normal1 = ResourceManager.Load<Texture>(definition.Get("normal1"));
				if (!string.IsNullOrWhiteSpace(definition.Get("normal2")))
					material.Normal2 = ResourceManager.Load<Texture>(definition.Get("normal2"));
				if (!string.IsNullOrWhiteSpace(definition.Get("normal3")))
					material.Normal3 = ResourceManager.Load<Texture>(definition.Get("normal3"));
				if (!string.IsNullOrWhiteSpace(definition.Get("normal4")))
					material.Normal4 = ResourceManager.Load<Texture>(definition.Get("normal4"));
				if (!string.IsNullOrWhiteSpace(definition.Get("splat")))
					material.Splat = ResourceManager.Load<Texture>(definition.Get("splat"));

				var shader = DefaultSplatShader;
				if (!string.IsNullOrEmpty(definition.Get("shader")))
					shader = definition.Get("shader");

				material.Shader = ResourceManager.Load<ShaderProgram>(shader);

				onLoaded(material);
			}
			else
			{
				var material = (Materials.StandardMaterial)resource;
				var filename = resource.Name + ".mat.v";

				var definition = material.Definition;
				var shaderOptions = new List<string>();

				if (material.IsSkinned)
					shaderOptions.Add("SKINNED");

				if (!string.IsNullOrWhiteSpace(definition.Get("diffuse-map")))
				{
					shaderOptions.Add("DIFFUSE_MAP");
					material.Diffuse = ResourceManager.Load<Texture>(definition.Get("diffuse-map"), "srgb");
				}
				else if (!string.IsNullOrWhiteSpace(definition.Get("diffuse-color")))
				{
					shaderOptions.Add("MATERIAL_DIFFUSE_COLOR");
					material.DiffuseColor = Common.StringConverter.Parse<Vector3>(definition.Get("diffuse-color"));
				}

				if (!string.IsNullOrWhiteSpace(definition.Get("normal-map")))
				{
					shaderOptions.Add("NORMAL_MAP");
					material.Normal = ResourceManager.Load<Texture>(definition.Get("normal-map"), "srgb");
				}

				if (!string.IsNullOrWhiteSpace(definition.Get("metallic")))
				{
					shaderOptions.Add("MATERIAL_METALLIC_VALUE");
					material.MetallicValue = Common.StringConverter.Parse<float>(definition.Get("metallic"));
				}

				if (!string.IsNullOrWhiteSpace(definition.Get("roughness")))
				{
					shaderOptions.Add("MATERIAL_ROUGHNESS_VALUE");
					material.RoughnessValue = Common.StringConverter.Parse<float>(definition.Get("roughness"));
				}

				if (!string.IsNullOrWhiteSpace(definition.Get("specular")))
				{
					shaderOptions.Add("MATERIAL_SPECULAR_VALUE");
					material.SpecularValue = Common.StringConverter.Parse<float>(definition.Get("specular"));
				}

				var shader = DefaultShader;
				if (!string.IsNullOrEmpty(definition.Get("shader")))
					shader = definition.Get("shader");

				material.Shader = ResourceManager.Load<ShaderProgram>(shader, shaderOptions.Join(","));

				onLoaded(material);
			}
		}

		public void Unload(Common.Resource resource)
		{
			var material = (Material)resource;
			material.Unload();
		}

		class MaterialDefintion
		{
			public string diffuse { get; set; }
			public string normal { get; set; }
			public string gloss { get; set; }
			public string specular { get; set; }
			public string shader { get; set; }
		}
	}
}
