using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Triton
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector3 : IEquatable<Vector3>
	{
		public float X;
		public float Y;
		public float Z;

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

		public Vector3(Vector4 v)
			: this(v.X, v.Y, v.Z)
		{

		}

		public float Length
		{
			get
			{
				return (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);
			}
		}

		public float LengthSquared
		{
			get
			{
				return X * X + Y * Y + Z * Z;
			}
		}

		public Vector3 Normalize()
		{
			Vector3 res;
			Normalize(ref this, out res);
			return res;
		}

		public static Vector3 Normalize(Vector3 v)
		{
			return v.Normalize();
		}

		public static void Normalize(ref Vector3 v, out Vector3 res)
		{
			var l = v.Length;
			res = new Vector3(v.X / l, v.Y / l, v.Z / l);
		}

		/// <summary>
		/// Calculate the dot (scalar) product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <returns>The dot product of the two inputs</returns>
		public static float Dot(Vector3 left, Vector3 right)
		{
			return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
		}

		/// <summary>
		/// Calculate the dot (scalar) product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <param name="result">The dot product of the two inputs</param>
		public static void Dot(ref Vector3 left, ref Vector3 right, out float result)
		{
			result = left.X * right.X + left.Y * right.Y + left.Z * right.Z;
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

		public static Vector3 Cross(Vector3 a, Vector3 b)
		{
			Vector3 res;
			Cross(ref a, ref b, out res);
			return res;
		}

		public static void Cross(ref Vector3 a, ref Vector3 b, out Vector3 res)
		{
			res = new Vector3(
				a.Y * b.Z - a.Z * b.Y,
				a.Z * b.X - a.X * b.Z,
				a.X * b.Y - a.Y * b.X);
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

		public static Vector3 operator -(Vector3 v)
		{
			return new Vector3(-v.X, -v.Y, -v.Z);
		}

		public override string ToString()
		{
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}, {2}", X, Y, Z);
		}

		/// <summary>Transform a direction vector by the given Matrix
		/// Assumes the matrix has a bottom row of (0,0,0,1), that is the translation part is ignored.
		/// </summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed vector</returns>
		public static Vector3 TransformVector(Vector3 vec, Matrix4 mat)
		{
			Vector3 v;
			v.X = Vector3.Dot(vec, new Vector3(mat.Column0));
			v.Y = Vector3.Dot(vec, new Vector3(mat.Column1));
			v.Z = Vector3.Dot(vec, new Vector3(mat.Column2));
			return v;
		}

		/// <summary>Transform a direction vector by the given Matrix
		/// Assumes the matrix has a bottom row of (0,0,0,1), that is the translation part is ignored.
		/// </summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed vector</param>
		public static void TransformVector(ref Vector3 vec, ref Matrix4 mat, out Vector3 result)
		{
			result.X = vec.X * mat.Row0.X +
					   vec.Y * mat.Row1.X +
					   vec.Z * mat.Row2.X;

			result.Y = vec.X * mat.Row0.Y +
					   vec.Y * mat.Row1.Y +
					   vec.Z * mat.Row2.Y;

			result.Z = vec.X * mat.Row0.Z +
					   vec.Y * mat.Row1.Z +
					   vec.Z * mat.Row2.Z;
		}

		/// <summary>Transform a Normal by the given Matrix</summary>
		/// <remarks>
		/// This calculates the inverse of the given matrix, use TransformNormalInverse if you
		/// already have the inverse to avoid this extra calculation
		/// </remarks>
		/// <param name="norm">The normal to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed normal</returns>
		public static Vector3 TransformNormal(Vector3 norm, Matrix4 mat)
		{
			mat.Invert();
			return TransformNormalInverse(norm, mat);
		}

		/// <summary>Transform a Normal by the given Matrix</summary>
		/// <remarks>
		/// This calculates the inverse of the given matrix, use TransformNormalInverse if you
		/// already have the inverse to avoid this extra calculation
		/// </remarks>
		/// <param name="norm">The normal to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed normal</param>
		public static void TransformNormal(ref Vector3 norm, ref Matrix4 mat, out Vector3 result)
		{
			Matrix4 Inverse = Matrix4.Invert(mat);
			Vector3.TransformNormalInverse(ref norm, ref Inverse, out result);
		}

		/// <summary>Transform a Normal by the (transpose of the) given Matrix</summary>
		/// <remarks>
		/// This version doesn't calculate the inverse matrix.
		/// Use this version if you already have the inverse of the desired transform to hand
		/// </remarks>
		/// <param name="norm">The normal to transform</param>
		/// <param name="invMat">The inverse of the desired transformation</param>
		/// <returns>The transformed normal</returns>
		public static Vector3 TransformNormalInverse(Vector3 norm, Matrix4 invMat)
		{
			Vector3 n;
			n.X = Vector3.Dot(norm, new Vector3(invMat.Row0));
			n.Y = Vector3.Dot(norm, new Vector3(invMat.Row1));
			n.Z = Vector3.Dot(norm, new Vector3(invMat.Row2));
			return n;
		}

		/// <summary>Transform a Normal by the (transpose of the) given Matrix</summary>
		/// <remarks>
		/// This version doesn't calculate the inverse matrix.
		/// Use this version if you already have the inverse of the desired transform to hand
		/// </remarks>
		/// <param name="norm">The normal to transform</param>
		/// <param name="invMat">The inverse of the desired transformation</param>
		/// <param name="result">The transformed normal</param>
		public static void TransformNormalInverse(ref Vector3 norm, ref Matrix4 invMat, out Vector3 result)
		{
			result.X = norm.X * invMat.Row0.X +
					   norm.Y * invMat.Row0.Y +
					   norm.Z * invMat.Row0.Z;

			result.Y = norm.X * invMat.Row1.X +
					   norm.Y * invMat.Row1.Y +
					   norm.Z * invMat.Row1.Z;

			result.Z = norm.X * invMat.Row2.X +
					   norm.Y * invMat.Row2.Y +
					   norm.Z * invMat.Row2.Z;
		}

		/// <summary>Transform a Position by the given Matrix</summary>
		/// <param name="pos">The position to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed position</returns>
		public static Vector3 TransformPosition(Vector3 pos, Matrix4 mat)
		{
			Vector3 p;
			p.X = Vector3.Dot(pos, new Vector3(mat.Column0)) + mat.Row3.X;
			p.Y = Vector3.Dot(pos, new Vector3(mat.Column1)) + mat.Row3.Y;
			p.Z = Vector3.Dot(pos, new Vector3(mat.Column2)) + mat.Row3.Z;
			return p;
		}

		/// <summary>Transform a Position by the given Matrix</summary>
		/// <param name="pos">The position to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed position</param>
		public static void TransformPosition(ref Vector3 pos, ref Matrix4 mat, out Vector3 result)
		{
			result.X = pos.X * mat.Row0.X +
					   pos.Y * mat.Row1.X +
					   pos.Z * mat.Row2.X +
					   mat.Row3.X;

			result.Y = pos.X * mat.Row0.Y +
					   pos.Y * mat.Row1.Y +
					   pos.Z * mat.Row2.Y +
					   mat.Row3.Y;

			result.Z = pos.X * mat.Row0.Z +
					   pos.Y * mat.Row1.Z +
					   pos.Z * mat.Row2.Z +
					   mat.Row3.Z;
		}

		/// <summary>Transform a Vector by the given Matrix</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed vector</returns>
		public static Vector3 Transform(Vector3 vec, Matrix4 mat)
		{
			Vector3 result;
			Transform(ref vec, ref mat, out result);
			return result;
		}

		/// <summary>Transform a Vector by the given Matrix</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed vector</param>
		public static void Transform(ref Vector3 vec, ref Matrix4 mat, out Vector3 result)
		{
			Vector4 v4 = new Vector4(vec.X, vec.Y, vec.Z, 1.0f);
			Vector4.Transform(ref v4, ref mat, out v4);
			result = v4.Xyz;
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <returns>The result of the operation.</returns>
		public static Vector3 Transform(Vector3 vec, Quaternion quat)
		{
			Vector3 result;
			Transform(ref vec, ref quat, out result);
			return result;
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <param name="result">The result of the operation.</param>
		public static void Transform(ref Vector3 vec, ref Quaternion quat, out Vector3 result)
		{
			// Since vec.W == 0, we can optimize quat * vec * quat^-1 as follows:
			// vec + 2.0 * cross(quat.xyz, cross(quat.xyz, vec) + quat.w * vec)
			Vector3 xyz = quat.Xyz, temp, temp2;
			Vector3.Cross(ref xyz, ref vec, out temp);
			Vector3.Multiply(ref vec, quat.W, out temp2);
			Vector3.Add(ref temp, ref temp2, out temp);
			Vector3.Cross(ref xyz, ref temp, out temp);
			Vector3.Multiply(ref temp, 2, out temp);
			Vector3.Add(ref vec, ref temp, out result);
		}

		/// <summary>Transform a Vector3 by the given Matrix, and project the resulting Vector4 back to a Vector3</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed vector</returns>
		public static Vector3 TransformPerspective(Vector3 vec, Matrix4 mat)
		{
			Vector3 result;
			TransformPerspective(ref vec, ref mat, out result);
			return result;
		}

		/// <summary>Transform a Vector3 by the given Matrix, and project the resulting Vector4 back to a Vector3</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed vector</param>
		public static void TransformPerspective(ref Vector3 vec, ref Matrix4 mat, out Vector3 result)
		{
			Vector4 v = new Vector4(vec, 1);
			Vector4.Transform(ref v, ref mat, out v);
			result.X = v.X / v.W;
			result.Y = v.Y / v.W;
			result.Z = v.Z / v.W;
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="scale">The scalar.</param>
		/// <param name="vec">The instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static Vector3 operator *(float scale, Vector3 vec)
		{
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			return vec;
		}

		public static bool operator ==(Vector3 left, Vector3 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Vector3 left, Vector3 right)
		{
			return !left.Equals(right);
		}

		public override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Vector3))
				return false;

			return this.Equals((Vector3)obj);
		}

		public bool Equals(Vector3 other)
		{
			return
				X == other.X &&
				Y == other.Y &&
				Z == other.Z;
		}
	}
}
