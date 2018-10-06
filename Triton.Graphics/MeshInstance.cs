using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public class MeshInstance
	{
		public Resources.Mesh Mesh;
		public SkeletalAnimation.SkeletonInstance Skeleton;
		public Matrix4 World = Matrix4.Identity;
		public bool CastShadows = true;
        public bool OwnsMesh = true;
    }
}
