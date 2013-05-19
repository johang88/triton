using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Triton
{
	[StructLayout(LayoutKind.Sequential)]
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

		public Vector3 Normalize()
		{
			Vector3 res;
			Normalize(ref this, out res);
			return res;
		}

		public static void Normalize(ref Vector3 v, out Vector3 res)
		{
			var l = v.Length;
			res = new Vector3(v.X / l, v.Y / l, v.Z / l);
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

		public static Vector3 operator +(Vector3 a, Vector3 b)
		{
			Vector3 res;
			Vector3.Add(ref a, ref b, out res);
			return res;
		}

		public static Vector3 operator -(Vector3 a, Vector3 b)
		{
			Vector3 res;
			Vector3.Subtract(ref a, ref b, out res);
			return res;
		}

		public static Vector3 operator *(Vector3 a, float b)
		{
			Vector3 res;
			Vector3.Multiply(ref a, b, out res);
			return res;
		}

		public static Vector3 operator /(Vector3 a, float b)
		{
			Vector3 res;
			Vector3.Divide(ref a, b, out res);
			return res;
		}
	}
}
