using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Triton.Common;
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
				material.Samplers = new Dictionary<string, string>(Context.Samplers);
			}

			var shaderOutputPath = System.IO.Path.GetDirectoryName(outputPath);

			var murmur128 = MurmurHash.Create128(managed: false);
			var shader = Encoding.UTF8.GetBytes(material.ShaderSource);
			var hash = murmur128.ComputeHash(shader);

			var shaderName = BitConverter.ToString(hash).ToLowerInvariant().Replace("-", "");
			shaderOutputPath = Path.Combine(shaderOutputPath, shaderName + ".glsl");

			if (!File.Exists(shaderOutputPath))
			{
				using (var stream = File.Open(shaderOutputPath, FileMode.Create))
				using (var writer = new StreamWriter(stream))
				{
					writer.Write(material.ShaderSource);
				}
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

				// This is wrong ... 
				writer.Write(shaderOutputPath);

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
