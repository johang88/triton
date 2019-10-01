using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Triton.Content.Meshes;
using Triton.Tools;
using Triton.IO;

namespace Triton.Content.Compilers
{
	public class MeshCompiler : ICompiler
	{
        public string Extension => ".mesh";
        public int Version => 1;

        private Factory<string, IMeshImporter> ImporterFactory;

		const int FormatVersion = 0x0160;

		public MeshCompiler()
		{
			ImporterFactory = new Factory<string, IMeshImporter>();
			ImporterFactory.Add(".xml", () => new Meshes.Converters.OgreXmlConverter());
			ImporterFactory.Add(".dae", () => new Meshes.Converters.AssimpConverter());
			ImporterFactory.Add(".fbx", () => new Meshes.Converters.AssimpConverter());
			ImporterFactory.Add(".x", () => new Meshes.Converters.AssimpConverter());
		}

		public void Compile(CompilationContext context)
		{
			string extension = Path.GetExtension(context.InputPath.Replace(".mesh.xml", ".xml")).ToLowerInvariant();

			var importer = ImporterFactory.Create(extension);
			var mesh = importer.Import(context.InputPath);

            if (mesh.Skeleton != null)
            {
                var skeletonOutputPath = context.OutputPath.Replace(".mesh", ".skeleton");
                mesh.SkeletonPath = context.GetReferencePath(skeletonOutputPath.Replace(".skeleton", ""));
                SkeletonCompiler.SerializeSkeleton(skeletonOutputPath, mesh.Skeleton);
            }

			using (var stream = File.Open(context.OutputPath, FileMode.Create))
			using (var writer = new BinaryWriter(stream))
			{
				// Magic
				writer.Write('M');
				writer.Write('E');
				writer.Write('S');
				writer.Write('H');

				// Version
				writer.Write(FormatVersion);

				// Skeleton link
				if (!string.IsNullOrWhiteSpace(mesh.SkeletonPath))
				{
					writer.Write(true);
					writer.Write(mesh.SkeletonPath);
				}
				else
				{
					writer.Write(false);
				}

				// Sub mesh count
				writer.Write(mesh.SubMeshes.Count);

				// Sub meshes
				foreach (var subMesh in mesh.SubMeshes)
				{
					// Material
					writer.Write(subMesh.Material);

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

                    // Bounds
                    writer.Write(subMesh.BoundingSphere.Radius);
                    writer.Write(subMesh.BoundingSphere.Center);
                    writer.Write(subMesh.BoundingBox.Min);
                    writer.Write(subMesh.BoundingBox.Max);

                    // Vertices
                    writer.Write(subMesh.Vertices);

					// Indices
					writer.Write(subMesh.Indices);
				}
			}
        }
	}
}
