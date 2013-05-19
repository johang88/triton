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

		public static readonly Vector3 Zero = new Vector3(0, 0, 0);
		public static readonly Vector3 UnitX = new Vector3(1, 0, 0);
		public static readonly Vector3 UnitY = new Vector3(0, 1, 0);
		public static readonly Vector3 UnitZ = new Vector3(0, 0, 1);

		public Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public float Length
		{
			get
			{
				return (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);
			}
		}

		public static void Normalize(ref Vector2 v, out Vector2 res)
		{
			var l = v.Length;
			res = new Vector2(v.X / l, v.Y / l);
		}

		public static float Dot(ref Vector3 a, ref Vector3 b)
		{
			return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
		}

		public static void Add(ref Vector3 a, ref Vector3 b, out Vector3 res)
		{
			res = new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		public static void Subtract(ref Vector3 a, ref Vector3 b, out Vector3 res)
		{
			res = new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}

		public static void Multiply(ref Vector3 a, float scalar, out Vector3 res)
		{
			res = new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
		}

		public static void Divide(ref Vector3 a, float scalar, out Vector3 res)
		{
			res = new Vector3(a.X / scalar, a.Y / scalar, a.Z / scalar);
		}
	}
}
