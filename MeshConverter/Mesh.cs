using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshConverter
{
	class Mesh
	{
		public List<SubMesh> SubMeshes = new List<SubMesh>();
	}

	class SubMesh
	{
		public int TriangleCount;
		public byte[] Vertices;
		public byte[] Indices;
	}
}
