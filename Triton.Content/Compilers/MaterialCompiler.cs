using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Triton.Content.Materials;
using NodeGraphControl;
using Triton.Content.Materials.CustomNodes;
using Triton.Content.Materials.DataTypes;
using Murmur;
using Triton.Content.Materials.NodeGraphDataTypes;

namespace Triton.Content.Compilers
{
	public class MaterialCompiler : ICompiler
	{
		const int Version = 0x01;

		public MaterialCompiler()
		{
		}

		public void Compile(CompilationContext context, string inputPath, string outputPath, Database.ContentEntry contentData)
		{
			outputPath += ".mat";

			Material material;
			using (var nodeGraphPanel = new NodeGraphPanel())
			{
				nodeGraphPanel.View.RegisterDataType(new NodeGraphDataTypeFloat());
				nodeGraphPanel.View.RegisterDataType(new NodeGraphDataTypeVector3());
				nodeGraphPanel.View.RegisterDataType(new NodeGraphDataTypeVector4());

				nodeGraphPanel.LoadCurrentViewPure(inputPath);

				RootNode root = null;
				foreach (var node in nodeGraphPanel.View.NodeCollection)
				{
					if (node is RootNode)
					{
						root = node as RootNode;
						break;
					}
				}

				var result = root.Process(0) as BuildShaderData;

				material = new Material();
				material.ShaderSource = result.Value;
				material.Defines = result.Defines;
				material.Samplers = new Dictionary<string, string>(Context.Samplers);
			}

			var murmur128 = MurmurHash.Create128(managed: false);
			var shader = Encoding.UTF8.GetBytes(material.ShaderSource);
			var hash = murmur128.ComputeHash(shader);

			var shaderName = BitConverter.ToString(hash).ToLowerInvariant().Replace("-", "");
			var shaderPath = "generated/shaders/" + shaderName;
			var shaderOutputPath = context.GetOutputPath(shaderPath + ".glsl");

			var template = context.GetShaderTemplate();
			material.ShaderSource = template.Replace("//__MATERIAL__PLACEHOLDER__", material.ShaderSource);
			if (material.Defines.Count > 0)
				material.ShaderSource = material.Defines.Select(d => "#define " + d).Aggregate((a, b) => a + "\n" + b) + "\n" + material.ShaderSource;

			using (var stream = context.OpenOutput(shaderPath + ".glsl"))
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(material.ShaderSource);
			}

			using (var stream = File.Open(outputPath, FileMode.Create))
			using (var writer = new BinaryWriter(stream))
			{
				// Magic
				writer.Write('M');
				writer.Write('A');
				writer.Write('T');
				writer.Write('E');

				// Version
				writer.Write(Version);

				// Shader reference
				writer.Write("/shaders/" + shaderName);

				// Samplers
				writer.Write(material.Samplers.Count);

				foreach (var sampler in material.Samplers)
				{
					writer.Write(sampler.Key);
					writer.Write(sampler.Value);
				}
			}
		}
	}
}
