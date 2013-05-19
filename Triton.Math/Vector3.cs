using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Math
{
	public struct Vector3
	{
		public readonly float X;
		public readonly float Y;
		public readonly float Z;

		public Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}
	}
}
