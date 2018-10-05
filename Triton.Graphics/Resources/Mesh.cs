using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;

namespace Triton.Graphics.Resources
{
	public class Mesh
	{
		public SubMesh[] SubMeshes { get; internal set; }
		public float BoundingSphereRadius;
		public SkeletalAnimation.Skeleton Skeleton = null;

        public Mesh()
		{
			SubMeshes = new SubMesh[0];
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
