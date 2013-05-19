using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Triton
{
	[StructLayout(LayoutKind.Sequential)]
    public struct Vector2
    {
		public float X;
		public float Y;

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

		public Vector2 Normalize()
		{
			Vector2 res;
			Normalize(ref this, out res);
			return res;
		}

		public static Vector2 Normalize(Vector2 v)
		{
			return v.Normalize();
		}

		public static void Normalize(ref Vector2 v, out Vector2 res)
		{
			var l = v.Length;
			res = new Vector2(v.X / l, v.Y / l);
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

		public static Vector2 operator +(Vector2 a, Vector2 b)
		{
			Vector2 res;
			Vector2.Add(ref a, ref b, out res);
			return res;
		}

		public static Vector2 operator -(Vector2 a, Vector2 b)
		{
			Vector2 res;
			Vector2.Subtract(ref a, ref b, out res);
			return res;
		}

		public static Vector2 operator *(Vector2 a, float b)
		{
			Vector2 res;
			Vector2.Multiply(ref a, b, out res);
			return res;
		}

		public static Vector2 operator /(Vector2 a, float b)
		{
			Vector2 res;
			Vector2.Divide(ref a, b, out res);
			return res;
		}

		public static Vector2 operator -(Vector2 v)
		{
			return new Vector2(-v.X, -v.Y);
		}

		public override string ToString()
		{
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}", X, Y);
		}
    }
}
