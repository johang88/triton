using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Compilers
{
	public class ShaderCompiler : ICompiler
	{
		public void Compile(string inputPath, string outputPath, ContentData contentData)
		{
			Shaders.Shader shader;

			using (var stream = System.IO.File.OpenRead(inputPath))
			{
				shader = ServiceStack.Text.JsonSerializer.DeserializeFromStream<Shaders.Shader>(stream);
			}

			var shaderBasePath = Path.GetDirectoryName(inputPath);
			shader.VertexShader = Path.Combine(shaderBasePath, shader.VertexShader);
			shader.FragmentShader = Path.Combine(shaderBasePath, shader.FragmentShader);

			var vertexShader = File.ReadAllText(shader.VertexShader);
			var fragmentShader = File.ReadAllText(shader.FragmentShader);

			var vertexBasePath = Path.GetDirectoryName(shader.VertexShader);
			var fragmentBasePath = Path.GetDirectoryName(shader.FragmentShader);

			vertexShader = new Shaders.ImportsResolver(vertexBasePath).Process(vertexShader);
			fragmentShader = new Shaders.ImportsResolver(fragmentBasePath).Process(fragmentShader);

			outputPath += ".glsl";
			using (var stream = File.Open(outputPath, FileMode.Create))
			using (var writer = new StreamWriter(stream))
			{
				writer.WriteLine("#version 330 core");
				foreach (var define in shader.Defines)
				{
					writer.WriteLine("#define " + define);
				}
				writer.WriteLine("#ifdef VERTEX_SHADER");
				writer.WriteLine(vertexShader);
				writer.WriteLine("#else");
				writer.WriteLine(fragmentShader);
				writer.WriteLine("#endif");
			}
		}
	}
}
