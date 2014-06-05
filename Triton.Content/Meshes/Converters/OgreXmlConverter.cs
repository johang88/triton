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
		public Mesh Import(string filename)
		{
			using (var stream = System.IO.File.OpenRead(filename))
			using (var reader = XmlReader.Create(stream))
			{
				var mesh = new Mesh();

				while (reader.ReadToFollowing("submesh"))
				{
					var subReader = reader.ReadSubtree();

					var subMesh = new SubMesh();
					subMesh.BoundingSphereRadius = 0;
					bool hasBones = false;
					int vertexCount = 0;
					Vertex[] vertices = null;

					subMesh.Material = reader.GetAttribute("material");

					while (subReader.Read())
					{
						if (subReader.NodeType != XmlNodeType.Element)
							continue;

						// Read faces
						if (subReader.Name == "faces")
						{
							int faceCount = StringConverter.Parse<int>(subReader.GetAttribute("count"));
							subMesh.TriangleCount = faceCount;

							using (var memStream = new MemoryStream(faceCount * sizeof(int) * 3))
							{
								using (var writer = new BinaryWriter(memStream))
								{
									for (int i = 0; i < faceCount; i++)
									{
										subReader.ReadToFollowing("face");

										writer.Write(StringConverter.Parse<int>(subReader.GetAttribute("v1")));
										writer.Write(StringConverter.Parse<int>(subReader.GetAttribute("v2")));
										writer.Write(StringConverter.Parse<int>(subReader.GetAttribute("v3")));
									}

									subMesh.Indices = memStream.GetBuffer();
								}
							}
						}
						// Read geometry
						else if (subReader.Name == "geometry")
						{
							if (vertexCount == 0)
							{
								vertexCount = StringConverter.Parse<int>(subReader.GetAttribute("vertexcount"));
								vertices = new Vertex[vertexCount];
							}

							var geometryReader = subReader.ReadSubtree();

							// Read vertex buffers
							while (geometryReader.Read())
							{
								if (geometryReader.NodeType != XmlNodeType.Element || geometryReader.Name != "vertexbuffer")
									continue;

								for (int i = 0; i < vertexCount; i++)
								{
									var vertex = vertices[i];

									if (!geometryReader.ReadToFollowing("vertex"))
										break;

									var vertexReader = geometryReader.ReadSubtree();

									while (vertexReader.Read())
									{
										if (vertexReader.NodeType != XmlNodeType.Element)
											continue;

										if (vertexReader.Name == "position")
											vertex.Position = ReadVector3(geometryReader);
										else if (vertexReader.Name == "normal")
											vertex.Normal = ReadVector3(geometryReader);
										else if (vertexReader.Name == "tangent")
											vertex.Tangent = ReadVector3(geometryReader);
										else if (vertexReader.Name == "texcoord")
											vertex.TexCoord = ReadUV(geometryReader);
									}

									vertices[i] = vertex;

									var length = vertex.Position.Length;
									if (subMesh.BoundingSphereRadius < length)
										subMesh.BoundingSphereRadius = length;
								}
							}
						}
						// Read bone assignments
						else if (subReader.Name == "boneassignments")
						{
							hasBones = true;

							var bonesReader = subReader.ReadSubtree();

							while (bonesReader.Read())
							{
								if (bonesReader.NodeType != XmlNodeType.Element || bonesReader.Name != "vertexboneassignment")
									continue;

								var index = StringConverter.Parse<int>(bonesReader.GetAttribute("vertexindex"));
								var vertex = vertices[index];

								var boneIndex = StringConverter.Parse<float>(bonesReader.GetAttribute("boneindex"));
								var boneWeight = StringConverter.Parse<float>(bonesReader.GetAttribute("weight"));

								if (vertex.BoneCount == 0)
								{
									vertex.BoneIndex.X = boneIndex;
									vertex.BoneWeight.X = boneWeight;
								}
								else if (vertex.BoneCount == 1)
								{
									vertex.BoneIndex.Y = boneIndex;
									vertex.BoneWeight.Y = boneWeight;
								}
								else if (vertex.BoneCount == 2)
								{
									vertex.BoneIndex.Z = boneIndex;
									vertex.BoneWeight.Z = boneWeight;
								}
								else if (vertex.BoneCount == 3)
								{
									vertex.BoneIndex.W = boneIndex;
									vertex.BoneWeight.W = boneWeight;
								}

								vertex.BoneCount++;
								vertices[index] = vertex;
							}
						}
					}

					if (hasBones)
					{
						subMesh.VertexFormat = new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
						{
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 3, 0),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Normal, Renderer.VertexPointerType.Float, 3, sizeof(float) * 3),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Tangent, Renderer.VertexPointerType.Float, 3, sizeof(float) * 6),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.TexCoord, Renderer.VertexPointerType.Float, 2, sizeof(float) * 9),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.BoneIndex, Renderer.VertexPointerType.Float, 4, sizeof(float) * 11),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.BoneWeight, Renderer.VertexPointerType.Float, 4, sizeof(float) * 15),
						});
					}
					else
					{
						subMesh.VertexFormat = new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
						{
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 3, 0),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Normal, Renderer.VertexPointerType.Float, 3, sizeof(float) * 3),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Tangent, Renderer.VertexPointerType.Float, 3, sizeof(float) * 6),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.TexCoord, Renderer.VertexPointerType.Float, 2, sizeof(float) * 9),
						});
					}

					using (var memStream = new MemoryStream(vertexCount * subMesh.VertexFormat.Size))
					{
						using (var writer = new BinaryWriter(memStream))
						{
							for (int i = 0; i < vertexCount; i++)
							{
								writer.Write(vertices[i].Position);
								writer.Write(vertices[i].Normal);
								writer.Write(vertices[i].Tangent);
								writer.Write(vertices[i].TexCoord);

								if (hasBones)
								{
									writer.Write(vertices[i].BoneIndex);
									writer.Write(vertices[i].BoneWeight);
								}
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
			return StringConverter.Parse<float>(value);
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
			public Vector4 BoneIndex;
			public Vector4 BoneWeight;
			public int BoneCount;
		}
	}
}
