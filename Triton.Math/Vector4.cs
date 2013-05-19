using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Triton
{
	[StructLayout(LayoutKind.Sequential)]
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

		public float Length
		{
			get
			{
				return (float)System.Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
			}
		}

		public Vector4 Normalize()
		{
			Vector4 res;
			Normalize(ref this, out res);
			return res;
		}

		public static Vector4 Normalize(Vector4 v)
		{
			return v.Normalize();
		}

		public static void Normalize(ref Vector4 v, out Vector4 res)
		{
			var l = v.Length;
			res = new Vector4(v.X / l, v.Y / l, v.Z / l, v.W / l);
		}

		public static float Dot(ref Vector4 a, ref Vector4 b)
		{
			return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
		}

		public static void Add(ref Vector4 a, ref Vector4 b, out Vector4 res)
		{
			res = new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
		}

		public static void Subtract(ref Vector4 a, ref Vector4 b, out Vector4 res)
		{
			res = new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
		}

		public static void Multiply(ref Vector4 a, float scalar, out Vector4 res)
		{
			res = new Vector4(a.X * scalar, a.Y * scalar, a.Z * scalar, a.W * scalar);
		}

		public static void Divide(ref Vector4 a, float scalar, out Vector4 res)
		{
			res = new Vector4(a.X / scalar, a.Y / scalar, a.Z / scalar, a.W / scalar);
		}

		public static Vector4 operator +(Vector4 a, Vector4 b)
		{
			Vector4 res;
			Vector4.Add(ref a, ref b, out res);
			return res;
		}

		public static Vector4 operator -(Vector4 a, Vector4 b)
		{
			Vector4 res;
			Vector4.Subtract(ref a, ref b, out res);
			return res;
		}

		public static Vector4 operator *(Vector4 a, float b)
		{
			Vector4 res;
			Vector4.Multiply(ref a, b, out res);
			return res;
		}

		public static Vector4 operator /(Vector4 a, float b)
		{
			Vector4 res;
			Vector4.Divide(ref a, b, out res);
			return res;
		}

		public static Vector4 operator -(Vector4 v)
		{
			return new Vector4(-v.X, -v.Y, -v.Z, -v.W);
		}
	}
}
