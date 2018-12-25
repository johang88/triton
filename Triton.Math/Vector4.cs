using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Triton
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector4 : IEquatable<Vector4>
	{
		public float X;
		public float Y;
		public float Z;
		public float W;

		public static readonly Vector4 Zero = new Vector4(0, 0, 0, 0);
		public static readonly Vector4 One = new Vector4(1, 1, 1, 1);
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

		public Vector4(Vector3 v, float w)
			: this(v.X, v.Y, v.Z, w)
		{
		}

		public float Length
		{
			get
			{
				return (float)System.Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
			}
		}

		public Vector3 Xyz
		{
			get { return new Vector3(X, Y, Z); }
			set { X = value.X; Y = value.Y; Z = value.Z; }
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

		public override string ToString()
		{
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", X, Y, Z, W);
		}

		/// <summary>Transform a Vector by the given Matrix</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed vector</returns>
		public static Vector4 Transform(Vector4 vec, Matrix4 mat)
		{
			Vector4 result;
			Transform(ref vec, ref mat, out result);
			return result;
		}

		/// <summary>Transform a Vector by the given Matrix</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed vector</param>
		public static void Transform(ref Vector4 vec, ref Matrix4 mat, out Vector4 result)
		{
			result = new Vector4(
				vec.X * mat.Row0.X + vec.Y * mat.Row1.X + vec.Z * mat.Row2.X + vec.W * mat.Row3.X,
				vec.X * mat.Row0.Y + vec.Y * mat.Row1.Y + vec.Z * mat.Row2.Y + vec.W * mat.Row3.Y,
				vec.X * mat.Row0.Z + vec.Y * mat.Row1.Z + vec.Z * mat.Row2.Z + vec.W * mat.Row3.Z,
				vec.X * mat.Row0.W + vec.Y * mat.Row1.W + vec.Z * mat.Row2.W + vec.W * mat.Row3.W);
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <returns>The result of the operation.</returns>
		public static Vector4 Transform(Vector4 vec, Quaternion quat)
		{
			Vector4 result;
			Transform(ref vec, ref quat, out result);
			return result;
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <param name="result">The result of the operation.</param>
		public static void Transform(ref Vector4 vec, ref Quaternion quat, out Vector4 result)
		{
			Quaternion v = new Quaternion(vec.X, vec.Y, vec.Z, vec.W), i, t;
			Quaternion.Invert(ref quat, out i);
			Quaternion.Multiply(ref quat, ref v, out t);
			Quaternion.Multiply(ref t, ref i, out v);

			result = new Vector4(v.X, v.Y, v.Z, v.W);
		}

        public static void Lerp(ref Vector4 a, ref Vector4 b, float blend, out Vector4 result)
        {
            result.X = blend * (b.X - a.X) + a.X;
            result.Y = blend * (b.Y - a.Y) + a.Y;
            result.Z = blend * (b.Z - a.Z) + a.Z;
            result.W = blend * (b.W - a.W) + a.W;
        }

        public static bool operator ==(Vector4 left, Vector4 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Vector4 left, Vector4 right)
		{
			return !left.Equals(right);
		}

		public override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Vector4))
				return false;

			return this.Equals((Vector4)obj);
		}

		public bool Equals(Vector4 other)
		{
			return
				X == other.X &&
				Y == other.Y &&
				Z == other.Z &&
				W == other.W;
		}

		public float this[int index]
		{
			get
			{
				if (index == 0) return X;
				else if (index == 1) return Y;
				else if (index == 2) return Z;
				else if (index == 3) return W;
				throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
			}
			set
			{
				if (index == 0) X = value;
				else if (index == 1) Y = value;
				else if (index == 2) Z = value;
				else if (index == 3) W = value;
				else throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
			}
		}
	}
}
