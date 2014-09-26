using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Triton.Common;
using Triton.Content.Meshes;

namespace Triton.Content.Compilers
{
	public class CollisionMeshCompiler : ICompiler
	{
		private Factory<string, IMeshImporter> ImporterFactory;

		const int Version = 0x0100;

		public CollisionMeshCompiler()
		{
			ImporterFactory = new Factory<string, IMeshImporter>();
			ImporterFactory.Add(".xml", () => new Meshes.Converters.OgreXmlConverter());
			ImporterFactory.Add(".dae", () => new Meshes.Converters.AssimpConverter());
			ImporterFactory.Add(".fbx", () => new Meshes.Converters.AssimpConverter());
		}

		public void Compile(CompilationContext context, string inputPath, string outputPath, Database.ContentEntry contentData)
		{
			outputPath += ".col";

			string extension = Path.GetExtension(inputPath.Replace(".mesh.xml", ".xml")).ToLowerInvariant();

			var importer = ImporterFactory.Create(extension);
			var mesh = importer.Import(inputPath);

			List<Vector3> vertices = new List<Vector3>();
			List<int> indices = new List<int>();

			foreach (var subMesh in mesh.SubMeshes)
			{
				var indexData = subMesh.Indices;
				var vertexData = subMesh.Vertices;
				var vertexFromat = subMesh.VertexFormat;

				Renderer.VertexFormatElement positionElement = null;
				foreach (var entry in vertexFromat.Elements)
				{
					if (entry.Semantic == Renderer.VertexFormatSemantic.Position)
					{
						positionElement = entry;
						break;
					}
				}

				var indexReader = new System.IO.BinaryReader(new System.IO.MemoryStream(indexData));
				var vertexReader = new System.IO.BinaryReader(new System.IO.MemoryStream(vertexData));

				int triangleCount = subMesh.Indices.Length / sizeof(int) / 3;
				for (int i = 0; i < triangleCount; i++)
				{
					var i0 = indexReader.ReadInt32();
					var i1 = indexReader.ReadInt32();
					var i2 = indexReader.ReadInt32();

					indices.Add(i0);
					indices.Add(i1);
					indices.Add(i2);
				}

				var vertexCount = vertexReader.BaseStream.Length / vertexFromat.Size;
				vertexReader.BaseStream.Position = positionElement.Offset;
				for (int i = 0; i < vertexCount; i++)
				{
					var vertex = vertexReader.ReadVector3();
					vertices.Add(vertex);

					vertexReader.BaseStream.Position += vertexFromat.Size;
				}

				indexReader.Dispose();
				vertexReader.Dispose();
			}

			using (var stream = File.Open(outputPath, FileMode.Create))
			using (var writer = new BinaryWriter(stream))
			{
				// Magic
				writer.Write('C');
				writer.Write('O');
				writer.Write('L');
				writer.Write('M');

				// Version
				writer.Write(Version);

				// No convex mesh support
				writer.Write(false);

				// Mesh data
				writer.Write(vertices.Count);
				writer.Write(indices.Count);

				foreach (var vertex in vertices)
				{
					writer.Write(vertex);
				}

				foreach (var index in indices)
				{
					writer.Write(index);
				}
			}
		}
	}
}
