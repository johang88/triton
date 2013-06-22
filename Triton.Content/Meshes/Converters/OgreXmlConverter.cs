using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Globalization;
using Triton.Common;

namespace Triton.Content.Meshes.Converters
{
	class OgreXmlConverter : IMeshImporter
	{
		public Mesh Import(Stream stream)
		{
			using (var reader = XmlReader.Create(stream))
			{
				var mesh = new Mesh();

				while (reader.ReadToFollowing("submesh"))
				{
					var subMesh = new SubMesh();

					var material = reader.GetAttribute("material");
					// TODO

					reader.ReadToFollowing("faces");
					int faceCount = int.Parse(reader.GetAttribute("count"), CultureInfo.InvariantCulture);

					using (var memStream = new MemoryStream(faceCount * sizeof(int) * 3))
					{
						using (var writer = new BinaryWriter(memStream))
						{
							for (int i = 0; i < faceCount; i++)
							{
								reader.ReadToFollowing("face");

								writer.Write(int.Parse(reader.GetAttribute("v1"), CultureInfo.InvariantCulture));
								writer.Write(int.Parse(reader.GetAttribute("v2"), CultureInfo.InvariantCulture));
								writer.Write(int.Parse(reader.GetAttribute("v3"), CultureInfo.InvariantCulture));
							}

							subMesh.Indices = memStream.GetBuffer();
						}
					}

					subMesh.TriangleCount = faceCount;

					subMesh.VertexFormat = new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
						{
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 3, 0),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Normal, Renderer.VertexPointerType.Float, 3, sizeof(float) * 3),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Tangent, Renderer.VertexPointerType.Float, 3, sizeof(float) * 6),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.TexCoord, Renderer.VertexPointerType.Float, 2, sizeof(float) * 9),
						});

					reader.ReadToFollowing("geometry");
					int vertexCount = int.Parse(reader.GetAttribute("vertexcount"), CultureInfo.InvariantCulture);

					var vertices = new Vertex[vertexCount];

					while (reader.ReadToFollowing("vertexbuffer"))
					{
						for (int i = 0; i < vertexCount; i++)
						{
							var vertex = vertices[i];

							reader.ReadToFollowing("vertex");
							var subReader = reader.ReadSubtree();

							while (subReader.Read())
							{
								if (subReader.NodeType != XmlNodeType.Element)
									continue;

								if (subReader.Name == "position")
									vertex.Position = ReadVector3(reader);
								else if (subReader.Name == "normal")
									vertex.Normal = ReadVector3(reader);
								else if (subReader.Name == "tangent")
									vertex.Tangent = ReadVector3(reader);
								else if (subReader.Name == "texcoord")
									vertex.TexCoord = ReadUV(reader);
							}

							vertices[i] = vertex;
						}
					}

					using (var memStream = new MemoryStream(vertexCount * (3 * sizeof(float) + 3 * sizeof(float) + 3 * sizeof(float) + 2 * sizeof(float))))
					{
						using (var writer = new BinaryWriter(memStream))
						{
							for (int i = 0; i < vertexCount; i++)
							{
								writer.Write(vertices[i].Position);
								writer.Write(vertices[i].Normal);
								writer.Write(vertices[i].Tangent);
								writer.Write(vertices[i].TexCoord);
							}

							subMesh.Vertices = memStream.GetBuffer();
						}
					}

					mesh.SubMeshes.Add(subMesh);
				}

				return mesh;
			}
		}

		float ParseFloat(string value)
		{
			return float.Parse(value, CultureInfo.InvariantCulture);
		}

		Triton.Vector3 ReadVector3(XmlReader reader)
		{
			return new Triton.Vector3(ParseFloat(reader.GetAttribute("x")), ParseFloat(reader.GetAttribute("y")), ParseFloat(reader.GetAttribute("z")));
		}

		Triton.Vector2 ReadUV(XmlReader reader)
		{
			return new Triton.Vector2(ParseFloat(reader.GetAttribute("u")), ParseFloat(reader.GetAttribute("v")));
		}

		struct Vertex
		{
			public Vector3 Position;
			public Vector3 Normal;
			public Vector3 Tangent;
			public Vector2 TexCoord;
		}
	}
}
