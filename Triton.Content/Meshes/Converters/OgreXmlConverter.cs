using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Globalization;
using Triton.Utility;
using Triton.IO;

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

				while (reader.Read())
				{
					var name = reader.Name;

					if (name == "submesh")
					{
						ReadSubMesh(reader, mesh);
					}
					else if (name == "skeletonlink")
					{
						mesh.SkeletonPath = reader.GetAttribute("name");
					}
				}

				return mesh;
			}
		}

        private void ReadSubMesh(XmlReader reader, Mesh mesh)
        {
            var subReader = reader.ReadSubtree();

            var subMesh = new SubMesh();
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

                            vertex.BoneAssignments = new List<BoneAssignment>();

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
                        }

                        subMesh.BoundingSphere = BoundingSphere.CreateFromPoints(vertices.Select(x => x.Position));
                    }

                    subMesh.BoundingBox = BoundingBox.CreateFromPoints(vertices.Select(x => x.Position));
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

                        var boneIndex = StringConverter.Parse<int>(bonesReader.GetAttribute("boneindex"));
                        var boneWeight = StringConverter.Parse<float>(bonesReader.GetAttribute("weight"));

                        vertex.BoneAssignments.Add(new BoneAssignment
                        {
                            BoneIndex = boneIndex,
                            Weight = boneWeight
                        });

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

            // Fix the bones and stuff
            for (int i = 0; i < vertexCount; i++)
            {
                // We need four!
                while (vertices[i].BoneAssignments.Count < 4)
                {
                    vertices[i].BoneAssignments.Add(new BoneAssignment());
                }

                // We only support 4 weight per vertex, drop the ones with the lowest weight
                if (vertices[i].BoneAssignments.Count > 4)
                {
                    vertices[i].BoneAssignments = vertices[i].BoneAssignments.OrderByDescending(b => b.Weight).Take(4).ToList();
                }

                // Normalize it
                var totalWeight = vertices[i].BoneAssignments.Sum(b => b.Weight);
                for (var b = 0; b < 4; b++)
                {
                    vertices[i].BoneAssignments[b].Weight = vertices[i].BoneAssignments[b].Weight / totalWeight;
                }
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
                            for (var b = 0; b < 4; b++)
                            {
                                writer.Write((float)vertices[i].BoneAssignments[b].BoneIndex);
                            }

                            for (var b = 0; b < 4; b++)
                            {
                                writer.Write(vertices[i].BoneAssignments[b].Weight);
                            }
                        }
                    }

                    subMesh.Vertices = memStream.GetBuffer();
                }
            }

            mesh.SubMeshes.Add(subMesh);
        }

        int SmallestIndex(Vector4 v)
        {
            int ix = 0;

            for (var i = 1; i < 4; i++)
            {
                if (v[i] < v[ix])
                {
                    ix = i;
                }
            }

            return ix;
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
            public List<BoneAssignment> BoneAssignments;
            public int BoneCount;
        }

        class BoneAssignment
        {
            public int BoneIndex;
            public float Weight;
        }
    }
}
