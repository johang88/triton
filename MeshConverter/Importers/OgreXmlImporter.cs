using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Xml;
using System.Globalization;
using System.IO;
using OpenTK.Graphics.OpenGL;
using Triton.Common;

namespace MeshConverter.Importers
{
	class OgreXmlImporter : IMeshImporter
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

					reader.ReadToFollowing("geometry");
					int vertexCount = int.Parse(reader.GetAttribute("vertexcount"), CultureInfo.InvariantCulture);

					reader.ReadToFollowing("vertexbuffer");
					bool positions = bool.Parse(reader.GetAttribute("positions"));
					bool normals = bool.Parse(reader.GetAttribute("normals"));
					bool tangents = bool.Parse(reader.GetAttribute("tangents"));
					int textureCoords = int.Parse(reader.GetAttribute("texture_coords"), CultureInfo.InvariantCulture);

					if (!positions)
						throw new ArgumentException("invalid mesh, no positions");

					if (!normals)
						throw new ArgumentException("invalid mesh, no normals");

					if (textureCoords == 0)
						throw new ArgumentException("invalid mesh, no texcoords");

					if (!tangents)
						throw new ArgumentException("invalid mesh, no tangents");

					using (var memStream = new MemoryStream(vertexCount * (3 * sizeof(float) + 3 * sizeof(float) + 3 * sizeof(float) + 2 * sizeof(float))))
					{
						using (var writer = new BinaryWriter(memStream))
						{
							for (int i = 0; i < vertexCount; i++)
							{
								reader.ReadToFollowing("vertex");

								reader.ReadToFollowing("position");
								writer.Write(ReadVector3(reader));

								reader.ReadToFollowing("normal");
								writer.Write(ReadVector3(reader));

								reader.ReadToFollowing("texcoord");
								var texCoord = ReadUV(reader);

								reader.ReadToFollowing("tangent");
								writer.Write(ReadVector3(reader));

								writer.Write(texCoord);
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

		Vector3 ReadVector3(XmlReader reader)
		{
			return new Vector3(ParseFloat(reader.GetAttribute("x")), ParseFloat(reader.GetAttribute("y")), ParseFloat(reader.GetAttribute("z")));
		}

		Vector2 ReadUV(XmlReader reader)
		{
			return new Vector2(ParseFloat(reader.GetAttribute("u")), ParseFloat(reader.GetAttribute("v")));
		}
	}
}
