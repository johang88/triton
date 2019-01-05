using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.IO;

namespace Triton.Graphics.Resources
{
    /// <summary>
    /// File format specification:
    /// 
    /// char[] Magic = "MESH"
    /// int Version = 0x0110
    /// int MeshCount
    /// 
    /// Mesh[MeshCount] Meshes
    ///		int TriangleCount
    ///		int VertexCount // Vertex data size in bytes
    ///		int IndexCount // Index data size in bytes
    ///		int VertexFormatElementsCount
    /// 
    ///		VertexFormatElement[VertexFormatElementsCount] VertexFormatELements
    ///			byte Semantic
    ///			int Type
    ///			byte Count
    ///			short Offset
    /// 
    ///		byte[VertexCount] VertexData
    ///		byte[IndexCount] IndexData
    /// </summary>
    class MeshSerializer : Triton.Resources.IResourceSerializer<Mesh>
    {
        static readonly char[] Magic = new char[] { 'M', 'E', 'S', 'H' };
        const int Version_1_4 = 0x0140;
        const int Version_1_5 = 0x0150;
        const int Version_1_6 = 0x0160;

        private readonly Backend _backend;
        private readonly Triton.IO.FileSystem _fileSystem;
        private readonly Triton.Resources.ResourceManager _resourceManager;

        public bool SupportsStreaming => false;

        public MeshSerializer(Backend backend, Triton.Resources.ResourceManager resourceManager, Triton.IO.FileSystem fileSystem)
        {
            _backend = backend ?? throw new ArgumentNullException("backend");
            _fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
            _resourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
        }

        public string Extension { get { return ".mesh"; } }
        public string DefaultFilename { get { return ""; } }

        public object Create(Type type)
             => new Mesh(_backend, _resourceManager);

        public async Task Deserialize(object resource, byte[] data)
        {
            var mesh = (Mesh)resource;
            mesh.Dispose();

            using (var stream = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                var magic = reader.ReadChars(4);
                for (var i = 0; i < Magic.Length; i++)
                {
                    if (magic[i] != Magic[i])
                        throw new ArgumentException("invalid mesh");
                }

                var version = reader.ReadInt32();

                var validVersions = new int[] { Version_1_4, Version_1_5, Version_1_6 };

                if (!validVersions.Contains(version))
                    throw new ArgumentException("invalid mesh, unknown version");

                var materialFlags = "";

                bool hasSkeleton = reader.ReadBoolean();
                if (hasSkeleton)
                {
                    var skeletonPath = reader.ReadString();
                    mesh.Skeleton = await _resourceManager.LoadAsync<SkeletalAnimation.Skeleton>(skeletonPath);
                    materialFlags = "SKINNED";
                }

                var meshCount = reader.ReadInt32();
                mesh.SubMeshes = new SubMesh[meshCount];

                for (var i = 0; i < meshCount; i++)
                {
                    var materialName = reader.ReadString();

                    Material material = null;
                    if (materialName != "no_material")
                        material = await _resourceManager.LoadAsync<Material>(materialName, materialFlags);

                    var triangleCount = reader.ReadInt32();
                    var vertexCount = reader.ReadInt32();
                    var indexCount = reader.ReadInt32();

                    Renderer.VertexFormat vertexFormat;

                    var vertexFormatElementCount = reader.ReadInt32();

                    var vertexFormatElements = new List<Renderer.VertexFormatElement>();
                    for (var j = 0; j < vertexFormatElementCount; j++)
                    {
                        vertexFormatElements.Add(new Renderer.VertexFormatElement(
                            (Renderer.VertexFormatSemantic)reader.ReadByte(),
                            (Renderer.VertexPointerType)reader.ReadInt32(),
                            reader.ReadByte(),
                            reader.ReadInt16()
                        ));
                    }

                    vertexFormat = new Renderer.VertexFormat(vertexFormatElements.ToArray());

                    var boundingSphere = new BoundingSphere(Vector3.Zero, reader.ReadSingle());
                    if (version >= Version_1_6)
                    {
                        reader.ReadVector3(ref boundingSphere.Center);
                    }

                    var boundingBox = BoundingBox.CreateFromSphere(boundingSphere); ;
                    if (version >= Version_1_5)
                    {
                        reader.ReadVector3(ref boundingBox.Min);
                        reader.ReadVector3(ref boundingBox.Max);
                    }

                    var vertices = reader.ReadBytes(vertexCount);
                    var indices = reader.ReadBytes(indexCount);

                    var subMesh = new SubMesh
                    {
                        VertexFormat = vertexFormat,
                        IndexData = indices,
                        VertexData = vertices,
                        TriangleCount = triangleCount,
                        Material = material,
                        BoundingSphere = boundingSphere,
                        BoundingBox = boundingBox,
                        VertexBufferHandle = _backend.RenderSystem.CreateBuffer(Renderer.BufferTarget.ArrayBuffer, false, vertexFormat),
                        IndexBufferHandle = _backend.RenderSystem.CreateBuffer(Renderer.BufferTarget.ElementArrayBuffer, false)
                    };

                    _backend.RenderSystem.SetBufferData(subMesh.VertexBufferHandle, vertices, false, false);
                    _backend.RenderSystem.SetBufferData(subMesh.IndexBufferHandle, indices, false, false);

                    subMesh.Handle = _backend.RenderSystem.CreateMesh(triangleCount, subMesh.VertexBufferHandle, subMesh.IndexBufferHandle, false);

                    mesh.SubMeshes[i] = subMesh;
                }
            }
        }

        public byte[] Serialize(object resource)
            => throw new NotImplementedException();
    }
}
