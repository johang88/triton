using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Triton
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Quaternion : IEquatable<Quaternion>
	{
		public Vector3 Xyz;
		public float w;

		public static readonly Quaternion Identity = new Quaternion(0, 0, 0, 1);

		public Quaternion(float x, float y, float z, float w)
		{
			Xyz = new Vector3(x, y, z);
			this.w = w;
		}

		public Quaternion(ref Matrix4 matrix)
		{
			var scale = System.Math.Pow(matrix.Determinant, 1.0 / 3.0);

			w = (float)System.Math.Sqrt(System.Math.Max(0, scale + matrix[0, 0] + matrix[1, 1] + matrix[2, 2])) / 2;
			Xyz.X = (float)System.Math.Sqrt(System.Math.Max(0, scale + matrix[0, 0] - matrix[1, 1] - matrix[2, 2])) / 2;
			Xyz.Y = (float)System.Math.Sqrt(System.Math.Max(0, scale - matrix[0, 0] + matrix[1, 1] - matrix[2, 2])) / 2;
			Xyz.Z = (float)System.Math.Sqrt(System.Math.Max(0, scale - matrix[0, 0] - matrix[1, 1] + matrix[2, 2])) / 2;
			if (matrix[2, 1] - matrix[1, 2] < 0) X = -X;
			if (matrix[0, 2] - matrix[2, 0] < 0) Y = -Y;
			if (matrix[1, 0] - matrix[0, 1] < 0) Z = -Z;
		}

		public Quaternion(Vector3 v, float w)
		{
			this.Xyz = v;
			this.w = w;
		}

		public float X { get { return Xyz.X; } set { Xyz.X = value; } }

		public float Y { get { return Xyz.Y; } set { Xyz.Y = value; } }

		public float Z { get { return Xyz.Z; } set { Xyz.Z = value; } }

		public float W { get { return w; } set { w = value; } }

		/// <summary>
		/// Convert the current quaternion to axis angle representation
		/// </summary>
		/// <param name="axis">The resultant axis</param>
		/// <param name="angle">The resultant angle</param>
		public void ToAxisAngle(out Vector3 axis, out float angle)
		{
			Vector4 result = ToAxisAngle();
			axis = result.Xyz;
			angle = result.W;
		}

		/// <summary>
		/// Convert this instance to an axis-angle representation.
		/// </summary>
		/// <returns>A Vector4 that is the axis-angle representation of this quaternion.</returns>
		public Vector4 ToAxisAngle()
		{
			Quaternion q = this;
			if (System.Math.Abs(q.W) > 1.0f)
				q.Normalize();

			Vector4 result = new Vector4();

			result.W = 2.0f * (float)System.Math.Acos(q.W); // angle
			float den = (float)System.Math.Sqrt(1.0 - q.W * q.W);
			if (den > 0.0001f)
			{
				result.Xyz = q.Xyz / den;
			}
			else
			{
				// This occurs when the angle is zero. 
				// Not a problem: just set an arbitrary normalized axis.
				result.Xyz = Vector3.UnitX;
			}

			return result;
		}

		/// <summary>
		/// Gets the length (magnitude) of the quaternion.
		/// </summary>
		/// <seealso cref="LengthSquared"/>
		public float Length
		{
			get
			{
				return (float)System.Math.Sqrt(W * W + Xyz.LengthSquared);
			}
		}

		/// <summary>
		/// Gets the square of the quaternion length (magnitude).
		/// </summary>
		public float LengthSquared
		{
			get
			{
				return W * W + Xyz.LengthSquared;
			}
		}

		/// <summary>
		/// Scales the Quaternion to unit length.
		/// </summary>
		public void Normalize()
		{
			float scale = 1.0f / this.Length;
			Xyz *= scale;
			W *= scale;
		}

		/// <summary>
		/// Convert this quaternion to its conjugate
		/// </summary>
		public void Conjugate()
		{
			Xyz = -Xyz;
		}

		/// <summary>
		/// Add two quaternions
		/// </summary>
		/// <param name="left">The first operand</param>
		/// <param name="right">The second operand</param>
		/// <returns>The result of the addition</returns>
		public static Quaternion Add(Quaternion left, Quaternion right)
		{
			return new Quaternion(
				left.Xyz + right.Xyz,
				left.W + right.W);
		}

		/// <summary>
		/// Add two quaternions
		/// </summary>
		/// <param name="left">The first operand</param>
		/// <param name="right">The second operand</param>
		/// <param name="result">The result of the addition</param>
		public static void Add(ref Quaternion left, ref Quaternion right, out Quaternion result)
		{
			result = new Quaternion(
				left.Xyz + right.Xyz,
				left.W + right.W);
		}

		/// <summary>
		/// Subtracts two instances.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <returns>The result of the operation.</returns>
		public static Quaternion Sub(Quaternion left, Quaternion right)
		{
			return new Quaternion(
				left.Xyz - right.Xyz,
				left.W - right.W);
		}

		/// <summary>
		/// Subtracts two instances.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <param name="result">The result of the operation.</param>
		public static void Sub(ref Quaternion left, ref Quaternion right, out Quaternion result)
		{
			result = new Quaternion(
				left.Xyz - right.Xyz,
				left.W - right.W);
		}

		/// <summary>
		/// Multiplies two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>A new instance containing the result of the calculation.</returns>
		public static Quaternion Multiply(Quaternion left, Quaternion right)
		{
			Quaternion result;
			Multiply(ref left, ref right, out result);
			return result;
		}

		/// <summary>
		/// Multiplies two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <param name="result">A new instance containing the result of the calculation.</param>
		public static void Multiply(ref Quaternion left, ref Quaternion right, out Quaternion result)
		{
			result = new Quaternion(
				right.W * left.Xyz + left.W * right.Xyz + Vector3.Cross(left.Xyz, right.Xyz),
				left.W * right.W - Vector3.Dot(left.Xyz, right.Xyz));
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="quaternion">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <param name="result">A new instance containing the result of the calculation.</param>
		public static void Multiply(ref Quaternion quaternion, float scale, out Quaternion result)
		{
			result = new Quaternion(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="quaternion">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>A new instance containing the result of the calculation.</returns>
		public static Quaternion Multiply(Quaternion quaternion, float scale)
		{
			return new Quaternion(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
		}

		/// <summary>
		/// Get the conjugate of the given quaternion
		/// </summary>
		/// <param name="q">The quaternion</param>
		/// <returns>The conjugate of the given quaternion</returns>
		public static Quaternion Conjugate(Quaternion q)
		{
			return new Quaternion(-q.Xyz, q.W);
		}

		/// <summary>
		/// Get the conjugate of the given quaternion
		/// </summary>
		/// <param name="q">The quaternion</param>
		/// <param name="result">The conjugate of the given quaternion</param>
		public static void Conjugate(ref Quaternion q, out Quaternion result)
		{
			result = new Quaternion(-q.Xyz, q.W);
		}

		/// <summary>
		/// Get the inverse of the given quaternion
		/// </summary>
		/// <param name="q">The quaternion to invert</param>
		/// <returns>The inverse of the given quaternion</returns>
		public static Quaternion Invert(Quaternion q)
		{
			Quaternion result;
			Invert(ref q, out result);
			return result;
		}

		/// <summary>
		/// Get the inverse of the given quaternion
		/// </summary>
		/// <param name="q">The quaternion to invert</param>
		/// <param name="result">The inverse of the given quaternion</param>
		public static void Invert(ref Quaternion q, out Quaternion result)
		{
			float lengthSq = q.LengthSquared;
			if (lengthSq != 0.0)
			{
				float i = 1.0f / lengthSq;
				result = new Quaternion(q.Xyz * -i, q.W * i);
			}
			else
			{
				result = q;
			}
		}

		/// <summary>
		/// Scale the given quaternion to unit length
		/// </summary>
		/// <param name="q">The quaternion to normalize</param>
		/// <returns>The normalized quaternion</returns>
		public static Quaternion Normalize(Quaternion q)
		{
			Quaternion result;
			Normalize(ref q, out result);
			return result;
		}

		/// <summary>
		/// Scale the given quaternion to unit length
		/// </summary>
		/// <param name="q">The quaternion to normalize</param>
		/// <param name="result">The normalized quaternion</param>
		public static void Normalize(ref Quaternion q, out Quaternion result)
		{
			float scale = 1.0f / q.Length;
			result = new Quaternion(q.Xyz * scale, q.W * scale);
		}

		/// <summary>
		/// Build a quaternion from the given axis and angle
		/// </summary>
		/// <param name="axis">The axis to rotate about</param>
		/// <param name="angle">The rotation angle in radians</param>
		/// <returns></returns>
		public static Quaternion FromAxisAngle(Vector3 axis, float angle)
		{
			if (axis.LengthSquared == 0.0f)
				return Identity;

			Quaternion result = Identity;

			angle *= 0.5f;
			axis.Normalize();
			result.Xyz = axis * (float)System.Math.Sin(angle);
			result.W = (float)System.Math.Cos(angle);

			return Normalize(result);
		}

		/// <summary>
		/// Do Spherical linear interpolation between two quaternions 
		/// </summary>
		/// <param name="q1">The first quaternion</param>
		/// <param name="q2">The second quaternion</param>
		/// <param name="blend">The blend factor</param>
		/// <returns>A smooth blend between the given quaternions</returns>
		public static void Slerp(ref Quaternion q1, ref Quaternion q2, float blend, out Quaternion res)
		{
			// if either input is zero, return the other.
			if (q1.LengthSquared == 0.0f)
			{
				if (q2.LengthSquared == 0.0f)
				{
					res = Identity;
				}
				res = q2;
			}
			else if (q2.LengthSquared == 0.0f)
			{
				res = q1;
			}


			float cosHalfAngle = q1.W * q2.W + Vector3.Dot(q1.Xyz, q2.Xyz);

			if (cosHalfAngle >= 1.0f || cosHalfAngle <= -1.0f)
			{
				// angle = 0.0f, so just return one input.
				res = q1;
			}
			else if (cosHalfAngle < 0.0f)
			{
				q2.Xyz = -q2.Xyz;
				q2.W = -q2.W;
				cosHalfAngle = -cosHalfAngle;
			}

			float blendA;
			float blendB;
			if (cosHalfAngle < 0.99f)
			{
				// do proper slerp for big angles
				float halfAngle = (float)System.Math.Acos(cosHalfAngle);
				float sinHalfAngle = (float)System.Math.Sin(halfAngle);
				float oneOverSinHalfAngle = 1.0f / sinHalfAngle;
				blendA = (float)System.Math.Sin(halfAngle * (1.0f - blend)) * oneOverSinHalfAngle;
				blendB = (float)System.Math.Sin(halfAngle * blend) * oneOverSinHalfAngle;
			}
			else
			{
				// do lerp if angle is really small.
				blendA = 1.0f - blend;
				blendB = blend;
			}

			Quaternion result = new Quaternion(blendA * q1.Xyz + blendB * q2.Xyz, blendA * q1.W + blendB * q2.W);
			if (result.LengthSquared > 0.0f)
				Normalize(ref result, out res);
			else
				res = Identity;
		}

		/// <summary>
		/// Adds two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static Quaternion operator +(Quaternion left, Quaternion right)
		{
			left.Xyz += right.Xyz;
			left.W += right.W;
			return left;
		}

		/// <summary>
		/// Subtracts two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static Quaternion operator -(Quaternion left, Quaternion right)
		{
			left.Xyz -= right.Xyz;
			left.W -= right.W;
			return left;
		}

		/// <summary>
		/// Multiplies two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static Quaternion operator *(Quaternion left, Quaternion right)
		{
			Multiply(ref left, ref right, out left);
			return left;
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="quaternion">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>A new instance containing the result of the calculation.</returns>
		public static Quaternion operator *(Quaternion quaternion, float scale)
		{
			Multiply(ref quaternion, scale, out quaternion);
			return quaternion;
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="quaternion">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>A new instance containing the result of the calculation.</returns>
		public static Quaternion operator *(float scale, Quaternion quaternion)
		{
			return new Quaternion(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
		}

		/// <summary>
		/// Compares two instances for equality.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>True, if left equals right; false otherwise.</returns>
		public static bool operator ==(Quaternion left, Quaternion right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Compares two instances for inequality.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>True, if left does not equal right; false otherwise.</returns>
		public static bool operator !=(Quaternion left, Quaternion right)
		{
			return !left.Equals(right);
		}

		/// <summary>
		/// Returns a System.String that represents the current Quaternion.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("V: {0}, W: {1}", Xyz, W);
		}


		/// <summary>
		/// Compares this object instance to another object for equality. 
		/// </summary>
		/// <param name="other">The other object to be used in the comparison.</param>
		/// <returns>True if both objects are Quaternions of equal value. Otherwise it returns false.</returns>
		public override bool Equals(object other)
		{
			if (other is Quaternion == false) return false;
			return this == (Quaternion)other;
		}

		/// <summary>
		/// Provides the hash code for this object. 
		/// </summary>
		/// <returns>A hash code formed from the bitwise XOR of this objects members.</returns>
		public override int GetHashCode()
		{
			return Xyz.GetHashCode() ^ W.GetHashCode();
		}

		/// <summary>
		/// Compares this Quaternion instance to another Quaternion for equality. 
		/// </summary>
		/// <param name="other">The other Quaternion to be used in the comparison.</param>
		/// <returns>True if both instances are equal; false otherwise.</returns>
		public bool Equals(Quaternion other)
		{
			return Xyz == other.Xyz && W == other.W;
		}
	}
}
