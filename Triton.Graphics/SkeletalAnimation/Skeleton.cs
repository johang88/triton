using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.SkeletalAnimation
{
	public class Skeleton
	{
		public Animation[] Animations;
		internal Transform[] BindPose;
		internal int[] BoneParents;

		public Transform[] GetInitialPose()
		{
			var bones = new Transform[BindPose.Length];
			BindPose.CopyTo(bones, 0);
			return bones;
		}
	}
}
