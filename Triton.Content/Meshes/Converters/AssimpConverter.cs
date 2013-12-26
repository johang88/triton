﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using Assimp.Configs;
using System.IO;
using Triton.Common;

namespace Triton.Content.Meshes.Converters
{
	class AssimpConverter : IMeshImporter
	{
		public Mesh Import(string filename)
		{
			using (var importer = new AssimpImporter())
			{
				importer.AttachLogStream(new LogStream((msg, userData) =>
				{
					Common.Log.WriteLine(msg);
				}));

				var mesh = new Mesh();

				importer.SetConfig(new Assimp.Configs.VertexBoneWeightLimitConfig(4));

				var model = importer.ImportFile(filename, PostProcessSteps.CalculateTangentSpace | PostProcessSteps.Triangulate 
					| PostProcessSteps.GenerateNormals | PostProcessSteps.PreTransformVertices | PostProcessSteps.LimitBoneWeights
					| PostProcessSteps.GenerateUVCoords | PostProcessSteps.TransformUVCoords);

				foreach (var meshToImport in model.Meshes)
				{
					// Validate sub mesh data
					if (meshToImport.PrimitiveType != PrimitiveType.Triangle)
					{
						Common.Log.WriteLine("{0}:{1} invalid primitive type {2} should be Triangle", filename, meshToImport.Name, meshToImport.PrimitiveType);
						continue;
					}

					if (!meshToImport.HasNormals)
					{
						Common.Log.WriteLine("{0}:{1} does not have any normals", filename, meshToImport.Name);
						continue;
					}

					if (!meshToImport.HasTangentBasis)
					{
						Common.Log.WriteLine("{0}:{1} does not have any tangents", filename, meshToImport.Name);
						continue;
					}

					if (meshToImport.TextureCoordsChannelCount == 0)
					{
						Common.Log.WriteLine("{0}:{1} does not have any texture channels", filename, meshToImport.Name);
						continue;
					}

					var subMesh = new SubMesh();

					// Create vertex format
					if (meshToImport.HasBones)
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

					Vertex[] vertices = new Vertex[meshToImport.VertexCount];

					var positions = meshToImport.Vertices;
					var normals = meshToImport.Normals;
					var tangents = meshToImport.Tangents;
					var texCoords = meshToImport.GetTextureCoords(0);

					// Setup vertex data
					for (var i = 0; i < vertices.Length; i++)
					{
						vertices[i].Position = new Vector3(positions[i].X, positions[i].Y, positions[i].Z);
						vertices[i].Normal = new Vector3(normals[i].X, normals[i].Y, normals[i].Z);
						vertices[i].Tangent = new Vector3(tangents[i].X, tangents[i].Y, tangents[i].Z);
						vertices[i].TexCoord = new Vector2(texCoords[i].X, texCoords[i].Y);
					}

					// Map bone weights if they are available
					if (meshToImport.HasBones)
					{
						var bones = meshToImport.Bones;

						for (var i = 0; i < bones.Length; i++)
						{
							var bone = bones[i];

							if (!bone.HasVertexWeights)
								continue;

							foreach (var weight in bone.VertexWeights)
							{
								var index = weight.VertexID;

								var vertex = vertices[index];

								if (vertex.BoneCount == 0)
								{
									vertex.BoneIndex.X = i;
									vertex.BoneWeight.X = weight.Weight;
								}
								else if (vertex.BoneCount == 1)
								{
									vertex.BoneIndex.Y = i;
									vertex.BoneWeight.Y = weight.Weight;
								}
								else if (vertex.BoneCount == 2)
								{
									vertex.BoneIndex.Z = i;
									vertex.BoneWeight.Z = weight.Weight;
								}
								else if (vertex.BoneCount == 3)
								{
									vertex.BoneIndex.W = i;
									vertex.BoneWeight.W = weight.Weight;
								}

								vertex.BoneCount++;
								vertices[index] = vertex;
							}
						}
					}

					using (var memStream = new MemoryStream(meshToImport.VertexCount * subMesh.VertexFormat.Size))
					{
						using (var writer = new BinaryWriter(memStream))
						{
							for (int i = 0; i < meshToImport.VertexCount; i++)
							{
								writer.Write(vertices[i].Position);
								writer.Write(vertices[i].Normal);
								writer.Write(vertices[i].Tangent);
								writer.Write(vertices[i].TexCoord);

								if (meshToImport.HasBones)
								{
									writer.Write(vertices[i].BoneIndex);
									writer.Write(vertices[i].BoneWeight);
								}
							}

							subMesh.Vertices = memStream.GetBuffer();
						}
					}

					using (var memStream = new MemoryStream(meshToImport.VertexCount * subMesh.VertexFormat.Size))
					{
						using (var writer = new BinaryWriter(memStream))
						{
							var indices = meshToImport.GetIntIndices();
							foreach (var index in indices)
							{
								writer.Write(index);
							}
							subMesh.Indices = memStream.GetBuffer();
						}
					}

					if (model.HasMaterials)
					{
						var material = model.Materials[meshToImport.MaterialIndex];
						subMesh.Material = material.Name;
					}
					else
					{
						subMesh.Material = "no_material";
					}

					mesh.SubMeshes.Add(subMesh);
				}

				return mesh;
			}
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