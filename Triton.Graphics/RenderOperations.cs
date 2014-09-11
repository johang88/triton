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
		private readonly RenderOperation[] Operations = new RenderOperation[MaxOperations];
		private int OperationsCount = 0;
		private readonly IComparer<RenderOperation> Comparer = new RenderOperationComparer();

		public void Add(int meshHandle, Matrix4 world, Resources.Material material, SkeletalAnimation.SkeletonInstance skeleton = null, bool useInstancing = false, bool castShadows = true)
		{
			Operations[OperationsCount].MeshHandle = meshHandle;
			Operations[OperationsCount].WorldMatrix = world;
			Operations[OperationsCount].Material = material;
			Operations[OperationsCount].Skeleton = skeleton;
			Operations[OperationsCount].UseInstancing = useInstancing;
            Operations[OperationsCount].CastShadows = castShadows;
            OperationsCount++;
		}

		public void Reset()
		{
			OperationsCount = 0;
		}

        public void GetOperations(out RenderOperation[] operations, out int count)
        {
			Array.Sort(Operations, 0, OperationsCount, Comparer);

            operations = Operations;
            count = OperationsCount;
        }

		class RenderOperationComparer : IComparer<RenderOperation>
		{
			public int Compare(RenderOperation x, RenderOperation y)
			{
				return x.Material.Id - y.Material.Id;
			}
		}
	}
}
