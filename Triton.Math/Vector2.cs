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

		public float Length
		{
			get
			{
				return (float)System.Math.Sqrt(X * X + Y * Y);
			}
		}

		public static void Normalize(ref Vector3 v, out Vector3 res)
		{
			var l = v.Length;
			res = new Vector3(v.X / l, v.Y / l, v.Z / l);
		}

		public static float Dot(ref Vector2 a, ref Vector2 b)
		{
			return a.X * b.X + a.Y * b.Y;
		}

		public static void Add(ref Vector2 a, ref Vector2 b, out Vector2 res)
		{
			res = new Vector2(a.X + b.X, a.Y + b.Y);
		}

		public static void Subtract(ref Vector2 a, ref Vector2 b, out Vector2 res)
		{
			res = new Vector2(a.X - b.X, a.Y - b.Y);
		}

		public static void Multiply(ref Vector2 a, float scalar, out Vector2 res)
		{
			res = new Vector2(a.X * scalar, a.Y * scalar);
		}

		public static void Divide(ref Vector2 a, float scalar, out Vector2 res)
		{
			res = new Vector2(a.X / scalar, a.Y / scalar);
		}
    }
}
