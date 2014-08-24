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
			using (var reader = new System.IO.StreamReader(stream))
			{
				var definition = JsonObject.Parse(reader.ReadToEnd());

				var isSkinned = Common.StringConverter.ParseBool(definition.Get("is_skinned") ?? "false");

				Material material = new Materials.StandardMaterial(name, parameters, ResourceManager, isSkinned);
				material.Definition = definition;

				return material;
			}
		}

		public void Load(Common.Resource resource, string parameters, Action<Common.Resource> onLoaded)
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
				material.Diffuse1 = ResourceManager.Load<Texture>(definition.Get("diffuse-map"), "srgb");
			}
			else if (!string.IsNullOrWhiteSpace(definition.Get("diffuse-cube")))
			{
				shaderOptions.Add("DIFFUSE_CUBE");
				material.DiffuseCube = ResourceManager.Load<Texture>(definition.Get("diffuse-cube"), "srgb");
			}
			else if (!string.IsNullOrWhiteSpace(definition.Get("diffuse-color")))
			{
				shaderOptions.Add("MATERIAL_DIFFUSE_COLOR");
				material.DiffuseColor = Common.StringConverter.Parse<Vector3>(definition.Get("diffuse-color"));
				material.DiffuseColor = material.DiffuseColor / 255.0f;
			}

			if (!string.IsNullOrWhiteSpace(definition.Get("normal-map")))
			{
				shaderOptions.Add("NORMAL_MAP");
				material.Normal1 = ResourceManager.Load<Texture>(definition.Get("normal-map"));
			}

			if (!string.IsNullOrEmpty(definition.Get("diffuse-splat-1")))
			{
				shaderOptions.Add("SPLAT");
				material.Diffuse1 = ResourceManager.Load<Texture>(definition.Get("diffuse-splat-1"), "srgb");
				material.Diffuse2 = ResourceManager.Load<Texture>(definition.Get("diffuse-splat-2"), "srgb");
				material.Diffuse3 = ResourceManager.Load<Texture>(definition.Get("diffuse-splat-3"), "srgb");
				material.Diffuse4 = ResourceManager.Load<Texture>(definition.Get("diffuse-splat-4"), "srgb");

				material.Normal1 = ResourceManager.Load<Texture>(definition.Get("normal-splat-1"));
				material.Normal2 = ResourceManager.Load<Texture>(definition.Get("normal-splat-2"));
				material.Normal3 = ResourceManager.Load<Texture>(definition.Get("normal-splat-3"));
				material.Normal4 = ResourceManager.Load<Texture>(definition.Get("normal-splat-4"));

				material.Splat = ResourceManager.Load<Texture>(definition.Get("splat-map"), "srgb");
			}

			if (!string.IsNullOrWhiteSpace(definition.Get("metallic")))
			{
				shaderOptions.Add("MATERIAL_METALLIC_VALUE");
				material.MetallicValue = Common.StringConverter.Parse<float>(definition.Get("metallic"));
			}

			if (!string.IsNullOrWhiteSpace(definition.Get("roughness-map")))
			{
				shaderOptions.Add("MATERIAL_ROUGHNESS_MAP");
				material.Roughness = ResourceManager.Load<Texture>(definition.Get("roughness-map"), "srgb");
			}
			else if (!string.IsNullOrWhiteSpace(definition.Get("roughness")))
			{
				shaderOptions.Add("MATERIAL_ROUGHNESS_VALUE");
				material.RoughnessValue = Common.StringConverter.Parse<float>(definition.Get("roughness"));
			}

			if (!string.IsNullOrWhiteSpace(definition.Get("specular")))
			{
				shaderOptions.Add("MATERIAL_SPECULAR_VALUE");
				material.SpecularValue = Common.StringConverter.Parse<float>(definition.Get("specular"));
			}

			if (!string.IsNullOrWhiteSpace(definition.Get("uv-animation")))
			{
				shaderOptions.Add("ANIM_UV");
				material.UvAnimation = Common.StringConverter.Parse<Vector2>(definition.Get("uv-animation"));
			}

			if (!string.IsNullOrWhiteSpace(definition.Get("lighting-mode")))
			{
				var mode = Common.StringConverter.Parse<LightingMode>(definition.Get("lighting-mode"));
				if (mode == LightingMode.Unlit)
				{
					shaderOptions.Add("UNLIT");
				}
			}

			var shader = DefaultShader;
			if (!string.IsNullOrEmpty(definition.Get("shader")))
				shader = definition.Get("shader");

			material.Shader = ResourceManager.Load<ShaderProgram>(shader, shaderOptions.Aggregate((a, b) => a + "," + b));

			onLoaded(material);
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
