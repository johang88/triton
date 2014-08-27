using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public class RenderOperations
	{
		const int MaxOperations = 8192;
		private RenderOperation[] Operations = new RenderOperation[MaxOperations];
		private int OperationsCount = 0;

		public void Add(int meshHandle, Matrix4 world, Resources.Material material, SkeletalAnimation.SkeletonInstance skeleton = null, bool useInstancing = false)
		{
			Operations[OperationsCount].MeshHandle = meshHandle;
			Operations[OperationsCount].WorldMatrix = world;
			Operations[OperationsCount].Material = material;
			Operations[OperationsCount].Skeleton = skeleton;
			Operations[OperationsCount].UseInstancing = useInstancing;
		}

		public void Reset()
		{
			OperationsCount = 0;
		}
	}
}
