using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Meshes
{
	class SubMesh
	{
		public string Material;
		public Renderer.VertexFormat VertexFormat;
		public int TriangleCount;
		public BoundingSphere BoundingSphere;
        public BoundingBox BoundingBox;
		public byte[] Vertices;
		public byte[] Indices;
	}
}
