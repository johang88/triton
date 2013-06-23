using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Skeletons
{
	class Skeleton
	{
		public List<Transform> Bones = new List<Transform>();
		public List<int> BoneParents = new List<int>();
		public List<Animation> Animations = new List<Animation>();
	}
}
