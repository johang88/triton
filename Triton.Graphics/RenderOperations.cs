using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int meshHandle, Matrix4 world, Resources.Material material, SkeletalAnimation.SkeletonInstance skeleton = null, bool useInstancing = false, bool castShadows = true)
		{
            var index = OperationsCount;
			Operations[index].MeshHandle = meshHandle;
			Operations[index].WorldMatrix = world;
			Operations[index].Material = material;
			Operations[index].Skeleton = skeleton;
			Operations[index].UseInstancing = useInstancing;
            Operations[index].CastShadows = castShadows;
            OperationsCount++;
		}

		public void Reset()
		{
			OperationsCount = 0;
		}

        public void GetOperations(out RenderOperation[] operations, out int count)
        {
            // TODO: Sort once we can optimize this
			//Array.Sort(Operations, 0, OperationsCount, Comparer);

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
