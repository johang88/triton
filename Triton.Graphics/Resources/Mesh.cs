using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;
using Triton.Renderer;

namespace Triton.Graphics.Resources
{
    public class Mesh : IDisposable
    {
        private Backend _backend;
        private ResourceManager _resourceManager;

        public SubMesh[] SubMeshes { get; set; }
        public float BoundingSphereRadius { get; set; }
        public SkeletalAnimation.Skeleton Skeleton { get; set; }

        public Mesh(Backend backend, ResourceManager resourceManager)
        {
            _backend = backend;
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

            SubMeshes = new SubMesh[0];
        }

        public void Dispose()
        {
            if (Skeleton != null)
            {
                _resourceManager.Unload(Skeleton);
            }

            foreach (var subMesh in SubMeshes)
            {
                _backend.RenderSystem.DestroyBuffer(subMesh.VertexBufferHandle);
                _backend.RenderSystem.DestroyBuffer(subMesh.IndexBufferHandle);
                _backend.RenderSystem.DestroyMesh(subMesh.Handle);

                if (subMesh.Material != null)
                {
                    _resourceManager.Unload(subMesh.Material);
                    subMesh.Material = null;
                }
            }

            SubMeshes = null;
        }
    }

    public class SubMesh
    {
        public Material Material;
        public float BoundingSphereRadius;
        public VertexFormat VertexFormat;
        public int TriangleCount;
        public byte[] VertexData;
        public byte[] IndexData;

        internal int VertexBufferHandle;
        internal int IndexBufferHandle;
        internal int Handle;
    }
}
