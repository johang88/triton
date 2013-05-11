using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;

namespace MeshConverter
{
	class Program
	{
		static string[] Parameters;
		static Factory<string, IMeshImporter> ImporterFactory;

		static bool GetParameter(string param)
		{
			foreach (var arg in Parameters)
			{
				if (arg == param)
					return true;
			}

			return false;
		}

		static string GetParameterString(string param)
		{
			foreach (var arg in Parameters)
			{
				if (arg.StartsWith(param))
				{
					return arg.Substring(param.Length);
				}
			}
			return "";
		}

		static void Main(string[] args)
		{
			Parameters = args;

			if (args.Length < 1)
			{
				Console.WriteLine("Usage: MeshConverter <filename>");
				return;
			}

			ImporterFactory = new Factory<string, IMeshImporter>();
			ImporterFactory.Add(".xml", () => new Importers.OgreXmlImporter());

			string filename = args[0].Trim();
			string extension = Path.GetExtension(filename.Replace(".mesh.xml", ".xml")).ToLowerInvariant();

			var importer = ImporterFactory.Create(extension);
			var mesh = importer.Import(File.OpenRead(filename));

			string outputPath = Path.ChangeExtension(filename.Replace(".mesh.xml", ".xml"), "mesh");

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
