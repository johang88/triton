﻿using System;
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
	public class MeshCompiler : ICompiler
	{
		private Factory<string, IMeshImporter> ImporterFactory;

		public MeshCompiler()
		{
			ImporterFactory = new Factory<string, IMeshImporter>();
			ImporterFactory.Add(".xml", () => new Meshes.Converters.OgreXmlConverter());
		}

		public void Compile(string inputPath, string outputPath)
		{
			outputPath = outputPath.Replace(".mesh.xml", ".xml");
			outputPath = Path.ChangeExtension(outputPath, "mesh");

			string extension = Path.GetExtension(inputPath.Replace(".mesh.xml", ".xml")).ToLowerInvariant();

			var importer = ImporterFactory.Create(extension);
			var mesh = importer.Import(File.OpenRead(inputPath));

			using (var stream = File.Open(outputPath, FileMode.Create))
			using (var writer = new BinaryWriter(stream))
			{
				// Magic
				writer.Write('M');
				writer.Write('E');
				writer.Write('S');
				writer.Write('H');

				// Version
				writer.Write(0x0100);

				// Sub mesh count
				writer.Write(mesh.SubMeshes.Count);

				// Sub meshes
				foreach (var subMesh in mesh.SubMeshes)
				{
					// Triangle count
					writer.Write(subMesh.TriangleCount);

					// Vertex count
					writer.Write(subMesh.Vertices.Length);

					// Index count
					writer.Write(subMesh.Indices.Length);

					// Vertices
					writer.Write(subMesh.Vertices);

					// Indices
					writer.Write(subMesh.Indices);
				}
			}
		}
	}
}