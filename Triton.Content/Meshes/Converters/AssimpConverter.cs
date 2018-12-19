using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using Assimp.Configs;
using System.IO;
using Triton.IO;
using Triton.Logging;

namespace Triton.Content.Meshes.Converters
{
    class AssimpConverter : IMeshImporter
    {
        public Mesh Import(string filename)
        {
            using (var importer = new AssimpContext())
            {
                var mesh = new Mesh();

                importer.SetConfig(new Assimp.Configs.VertexBoneWeightLimitConfig(4));

                var model = importer.ImportFile(filename, PostProcessSteps.CalculateTangentSpace | PostProcessSteps.Triangulate
                    | PostProcessSteps.GenerateNormals | PostProcessSteps.LimitBoneWeights | PostProcessSteps.FlipUVs);

                var nameToIndex = new Dictionary<string, int>();
                ParseSkeleton(mesh, model, nameToIndex);

                foreach (var meshToImport in model.Meshes)
                {
                    // Validate sub mesh data
                    if (meshToImport.PrimitiveType != PrimitiveType.Triangle)
                    {
                        Log.WriteLine("{0}:{1} invalid primitive type {2} should be Triangle", filename, meshToImport.Name, meshToImport.PrimitiveType);
                        continue;
                    }

                    if (!meshToImport.HasNormals)
                    {
                        Log.WriteLine("{0}:{1} does not have any normals", filename, meshToImport.Name);
                        continue;
                    }

                    if (!meshToImport.HasTangentBasis)
                    {
                        Log.WriteLine("{0}:{1} does not have any tangents", filename, meshToImport.Name);
                        continue;
                    }

                    if (meshToImport.TextureCoordinateChannelCount == 0)
                    {
                        Log.WriteLine("{0}:{1} does not have any texture channels", filename, meshToImport.Name);
                        continue;
                    }

                    var subMesh = new SubMesh();
                    subMesh.BoundingSphereRadius = 0;

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

                    subMesh.TriangleCount = meshToImport.FaceCount;

                    Vertex[] vertices = new Vertex[meshToImport.VertexCount];

                    var positions = meshToImport.Vertices;
                    var normals = meshToImport.Normals;
                    var tangents = meshToImport.Tangents;
                    var texCoords = meshToImport.TextureCoordinateChannels[0];

                    // Setup vertex data
                    for (var i = 0; i < vertices.Length; i++)
                    {
                        vertices[i].Position = new Vector3(positions[i].X, positions[i].Y, positions[i].Z);
                        vertices[i].Normal = new Vector3(normals[i].X, normals[i].Y, normals[i].Z);
                        vertices[i].Tangent = new Vector3(tangents[i].X, tangents[i].Y, tangents[i].Z);
                        vertices[i].TexCoord = new Vector2(texCoords[i].X, texCoords[i].Y);
                        vertices[i].BoneAssignments = new List<BoneAssignment>();

                        var length = vertices[i].Position.Length;
                        if (subMesh.BoundingSphereRadius < length)
                            subMesh.BoundingSphereRadius = length;
                    }

                    // Map bone weights if they are available
                    if (meshToImport.HasBones)
                    {
                        for (var i = 0; i < meshToImport.Bones.Count; i++)
                        {
                            var bone = meshToImport.Bones[i];

                            if (!bone.HasVertexWeights)
                                continue;

                            foreach (var weight in bone.VertexWeights)
                            {
                                var index = weight.VertexID;

                                vertices[index].BoneAssignments.Add(new BoneAssignment
                                {
                                    BoneIndex = nameToIndex[bone.Name],
                                    Weight = weight.Weight
                                });
                                vertices[index].BoneCount++;
                            }
                        }
                    }

                    // Fix the bones and stuff
                    for (int i = 0; i < vertices.Length; i++)
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

                    using (var memStream = new MemoryStream(meshToImport.VertexCount * subMesh.VertexFormat.Size))
                    {
                        using (var writer = new BinaryWriter(memStream))
                        {
                            for (int i = 0; i < vertices.Length; i++)
                            {
                                writer.Write(vertices[i].Position);
                                writer.Write(vertices[i].Normal);
                                writer.Write(vertices[i].Tangent);
                                writer.Write(vertices[i].TexCoord);

                                if (meshToImport.HasBones)
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

                    var indices = meshToImport.GetIndices();
                    using (var memStream = new MemoryStream(sizeof(int) * indices.Length))
                    {
                        using (var writer = new BinaryWriter(memStream))
                        {
                            foreach (var index in indices)
                            {
                                writer.Write(index);
                            }
                            subMesh.Indices = memStream.GetBuffer();
                        }
                    }

                    if (model.HasMaterials)
                    {
                        var material = model.Materials[meshToImport.MaterialIndex].Name;

                        if (!material.StartsWith("/materials/"))
                        {
                            material = "/materials/" + material;
                        }

                        subMesh.Material = material;
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

        private void ParseSkeleton(Mesh mesh, Scene model, Dictionary<string, int> nameToIndex)
        {
            // Create skeleton if any of the models have bones
            var bones = new Dictionary<string, Bone>();

            // Fetch all bones first
            foreach (var meshToImport in model.Meshes)
            {
                if (meshToImport.HasBones)
                {
                    foreach (var bone in meshToImport.Bones)
                    {
                        if (!bones.ContainsKey(bone.Name))
                        {
                            bones.Add(bone.Name, bone);
                        }
                    }
                }
            }

            if (bones.Any())
            {
                mesh.Skeleton = new Skeletons.Skeleton
                {
                    Bones = new List<Skeletons.Transform>(),
                    Animations = new List<Skeletons.Animation>()
                };
                
                // Find actual root node
                var rootNode = model.RootNode;
                if (!bones.ContainsKey(rootNode.Name))
                {
                    foreach (var child in rootNode.Children)
                    {
                        if (bones.ContainsKey(child.Name))
                        {
                            rootNode = child;
                            break;
                        }
                    }
                }

                var rootNodeName = rootNode.Name;

                // Skeleton system assumes that the root node is located at index 0, so we reserve a slot for it
                mesh.Skeleton.Bones.Add(new Skeletons.Transform());
                nameToIndex[rootNodeName] = 0;

                foreach (var bone in bones)
                {
                    var bindPose = bone.Value.OffsetMatrix;
                    bindPose.DecomposeNoScaling(out var rotation, out var transalation);

                    var transform = new Skeletons.Transform
                    {
                        Position = new Vector3(transalation.X, transalation.Y, transalation.Z),
                        Orientation = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W)
                    };

                    if (bone.Key == rootNodeName)
                    {
                        mesh.Skeleton.Bones[0] = transform;
                    }
                    else
                    {
                        mesh.Skeleton.Bones.Add(transform);
                        nameToIndex.Add(bone.Key, mesh.Skeleton.Bones.Count - 1);
                    }
                }

                // Parse bone hierarchy
                var parentIndexes = new Dictionary<string, int>();
                var nameToNode = new Dictionary<string, Node>();
                ParseParents(nameToIndex, parentIndexes, nameToNode, rootNode, true);

                foreach (var node in nameToNode.Values)
                {
                    var index = nameToIndex[node.Name];

                    node.Transform.DecomposeNoScaling(out var rotation, out var transalation);

                    mesh.Skeleton.Bones[index] = new Skeletons.Transform
                    {
                        Position = new Vector3(transalation.X, transalation.Y, transalation.Z),
                        Orientation = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W)
                    };
                }

                mesh.Skeleton.BoneParents = new List<int>(mesh.Skeleton.Bones.Count);
                for (var i = 0; i < mesh.Skeleton.Bones.Count; i++)
                {
                    mesh.Skeleton.BoneParents.Add(0);
                }

                foreach (var parentIndex in parentIndexes)
                {
                    if (nameToIndex.ContainsKey(parentIndex.Key))
                    {
                        mesh.Skeleton.BoneParents[nameToIndex[parentIndex.Key]] = parentIndex.Value;
                    }
                    else
                    {
                        Log.WriteLine($"Missing bone binding for node {parentIndex.Key}", LogLevel.Warning);
                    }
                }

                // Parse animations
                foreach (var animationToImport in model.Animations)
                {
                    var animation = new Skeletons.Animation
                    {
                        Name = animationToImport.Name,
                        Tracks = new List<Skeletons.Track>(),
                        Length = (float)(animationToImport.DurationInTicks / animationToImport.TicksPerSecond)
                    };

                    foreach (var nodeAnimation in animationToImport.NodeAnimationChannels)
                    {
                        // Skip missing bones
                        if (!bones.ContainsKey(nodeAnimation.NodeName))
                            continue;

                        var track = new Skeletons.Track
                        {
                            BoneIndex = nameToIndex[nodeAnimation.NodeName],
                            KeyFrames = new List<Skeletons.KeyFrame>()
                        };

                        var defBonePoseInv = nameToNode[nodeAnimation.NodeName].Transform;
                        defBonePoseInv.Inverse();

                        for (var i = 0; i < nodeAnimation.PositionKeys.Count; i++)
                        {
                            var position = nodeAnimation.PositionKeys[i].Value;
                            var rotation = nodeAnimation.RotationKeys[i].Value;

                            var fullTransform = new Matrix4x4(rotation.GetMatrix()) * Matrix4x4.FromTranslation(position);
                            var poseToKey = fullTransform * defBonePoseInv;
                            poseToKey.DecomposeNoScaling(out var rot, out var pos);

                            var time = nodeAnimation.PositionKeys[i].Time / animationToImport.TicksPerSecond;

                            track.KeyFrames.Add(new Skeletons.KeyFrame
                            {
                                Time = (float)time,
                                Transform = new Skeletons.Transform
                                {
                                    Position = new Vector3(pos.X, pos.Y, pos.Z),
                                    Orientation = new Quaternion(rot.X, rot.Y, rot.Z, rot.W)
                                }
                            });
                        }

                        animation.Tracks.Add(track);
                    }

                    mesh.Skeleton.Animations.Add(animation);
                }
            }
        }

        private void ParseParents(Dictionary<string, int> nameToIndex, Dictionary<string, int> parentIndexes, Dictionary<string, Node> nameToNode, Node node, bool isRootNode)
        {
            if (!nameToIndex.ContainsKey(node.Name))
            {
                return;
            }

            nameToNode.Add(node.Name, node);

            if (isRootNode)
            {
                parentIndexes.Add(node.Name, -1); // This is a root node!
            }
            else
            {
                parentIndexes.Add(node.Name, nameToIndex[node.Parent.Name]);
            }

            if (node.HasChildren)
            {
                foreach (var child in node.Children)
                {
                    ParseParents(nameToIndex, parentIndexes, nameToNode, child, false);
                }
            }
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
