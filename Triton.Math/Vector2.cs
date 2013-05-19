using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Math
{
    public struct Vector2
    {
		public readonly float X;
		public readonly float Y;

		public static readonly Vector2 Zero = new Vector2(0, 0);
		public static readonly Vector2 UnitX = new Vector2(1, 0);
		public static readonly Vector2 UnitY = new Vector2(0, 1);

		public Vector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		public static float Dot(ref Vector2 a, ref Vector2 b)
		{
			return a.X * b.X + a.Y * b.Y;
		}
    }
}
