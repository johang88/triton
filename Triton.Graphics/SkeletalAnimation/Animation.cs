using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.SkeletalAnimation
{
	class Animation
	{
		public readonly string Name;
		public readonly float Length;
		public Track[] Tracks;

		public Animation(string name, float length)
		{
			Name = name;
			Length = length;
		}
	}
}
