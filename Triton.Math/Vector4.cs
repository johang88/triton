using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Math
{
	public struct Vector4
	{
		public readonly float X;
		public readonly float Y;
		public readonly float Z;
		public readonly float W;

		public static readonly Vector4 Zero = new Vector4(0, 0, 0, 0);
		public static readonly Vector4 UnitX = new Vector4(1, 0, 0, 0);
		public static readonly Vector4 UnitY = new Vector4(0, 1, 0, 0);
		public static readonly Vector4 UnitZ = new Vector4(0, 0, 1, 0);
		public static readonly Vector4 UnitW = new Vector4(0, 0, 0, 1);

		public Vector4(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public static float Dot(ref Vector4 a, ref Vector4 b)
		{
			return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
		}
	}
}
