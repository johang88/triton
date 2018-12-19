using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Meshes
{
	class Mesh
	{
		public string SkeletonPath = null;
        public Skeletons.Skeleton Skeleton = null;
		public List<SubMesh> SubMeshes = new List<SubMesh>();
	}
}
