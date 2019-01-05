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

		private readonly RenderOperation[] _operations = new RenderOperation[MaxOperations];
		private int _operationsCount = 0;
		private readonly IComparer<RenderOperation> _comparer = new RenderOperationComparer();
        private Vector3 _positonZero = Vector3.Zero;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int meshHandle, Matrix4 world, Resources.Material material, SkeletalAnimation.SkeletonInstance skeleton = null, bool useInstancing = false, bool castShadows = true)
		{
            var index = _operationsCount;
			_operations[index].MeshHandle = meshHandle;
			_operations[index].WorldMatrix = world;
			_operations[index].Material = material;
			_operations[index].Skeleton = skeleton;
			_operations[index].UseInstancing = useInstancing;
            _operations[index].CastShadows = castShadows;

            //Vector3.Transform(ref _positonZero, ref world, out _operations[_operationsCount].PositionWS);

            _operationsCount++;
		}

		public void Reset()
		{
			_operationsCount = 0;
		}

        public void GetOperations(out RenderOperation[] operations, out int count)
        {
            operations = _operations;
            count = _operationsCount;
        }

        public void GetOperations(ref Matrix4 view, out RenderOperation[] operations, out int count)
        {
            //for (var i = 0; i < _operationsCount; i++)
            //{
            //    Vector3.Transform(ref _operations[i].PositionWS, ref view, out _operations[i].PositionVS);
            //}

            //Array.Sort(_operations, 0, _operationsCount, _comparer);

            operations = _operations;
            count = _operationsCount;
        }

        class RenderOperationComparer : IComparer<RenderOperation>
		{
			public int Compare(RenderOperation x, RenderOperation y)
			{
                if (y.PositionVS.Z < x.PositionVS.Z)
                    return -1;
                else
                    return 1;
            }
		}
	}
}
