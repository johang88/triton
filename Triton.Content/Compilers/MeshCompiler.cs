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
	public class MeshSettings
	{
		public string Material { get; set; }
	}

	public class MeshCompiler : ICompiler
	{
		private Factory<string, IMeshImporter> ImporterFactory;

		const int Version = 0x0130;

		public MeshCompiler()
		{
			ImporterFactory = new Factory<string, IMeshImporter>();
			ImporterFactory.Add(".xml", () => new Meshes.Converters.OgreXmlConverter());
			ImporterFactory.Add(".dae", () => new Meshes.Converters.AssimpConverter());
		}

		public void Compile(string inputPath, string outputPath, ContentData contentData)
		{
			outputPath += ".mesh";

			string extension = Path.GetExtension(inputPath.Replace(".mesh.xml", ".xml")).ToLowerInvariant();

			MeshSettings settings;

			if (contentData.Settings == null || !(contentData.Settings is MeshSettings))
			{
				settings = new MeshSettings();
				contentData.Settings = settings;
			}
			else
			{
				settings = contentData.Settings as MeshSettings;
			}

			var importer = ImporterFactory.Create(extension);
			var mesh = importer.Import(inputPath);

			using (var stream = File.Open(outputPath, FileMode.Create))
			using (var writer = new BinaryWriter(stream))
			{
				// Magic
				writer.Write('M');
				writer.Write('E');
				writer.Write('S');
				writer.Write('H');

				// Version
				writer.Write(Version);

				// Sub mesh count
				writer.Write(mesh.SubMeshes.Count);

				// Sub meshes
				foreach (var subMesh in mesh.SubMeshes)
				{
					// Material
					writer.Write(!string.IsNullOrWhiteSpace(settings.Material) ? settings.Material : subMesh.Material);

					// Triangle count
					writer.Write(subMesh.TriangleCount);

					// Vertex count
					writer.Write(subMesh.Vertices.Length);

					// Index count
					writer.Write(subMesh.Indices.Length);

					// Vertex format
					writer.Write(subMesh.VertexFormat.Elements.Length);
					foreach (var element in subMesh.VertexFormat.Elements)
					{
						writer.Write((byte)element.Semantic);
						writer.Write((int)element.Type);
						writer.Write(element.Count);
						writer.Write(element.Offset);
					}

					// Bounding sphere
					writer.Write(subMesh.BoundingSphereRadius);

					// Vertices
					writer.Write(subMesh.Vertices);

					// Indices
					writer.Write(subMesh.Indices);
				}
			}
		}
	}
}
