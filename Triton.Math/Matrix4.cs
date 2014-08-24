using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Triton
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Matrix4 : IEquatable<Matrix4>
	{
		public Vector4 Row0;
		public Vector4 Row1;
		public Vector4 Row2;
		public Vector4 Row3;

		public static readonly Matrix4 Identity = new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW);

		public Matrix4(Vector4 row0, Vector4 row1, Vector4 row2, Vector4 row3)
		{
			Row0 = row0;
			Row1 = row1;
			Row2 = row2;
			Row3 = row3;
		}

		public Matrix4(
			float m00, float m01, float m02, float m03,
			float m10, float m11, float m12, float m13,
			float m20, float m21, float m22, float m23,
			float m30, float m31, float m32, float m33
			)
		{
			Row0 = new Vector4(m00, m01, m02, m03);
			Row1 = new Vector4(m10, m11, m12, m13);
			Row2 = new Vector4(m20, m21, m22, m23);
			Row3 = new Vector4(m30, m31, m32, m33);
		}

		public float this[int rowIndex, int columnIndex]
		{
			get
			{
				if (rowIndex == 0) return Row0[columnIndex];
				else if (rowIndex == 1) return Row1[columnIndex];
				else if (rowIndex == 2) return Row2[columnIndex];
				else if (rowIndex == 3) return Row3[columnIndex];
				throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
			}
			set
			{
				if (rowIndex == 0) Row0[columnIndex] = value;
				else if (rowIndex == 1) Row1[columnIndex] = value;
				else if (rowIndex == 2) Row2[columnIndex] = value;
				else if (rowIndex == 3) Row3[columnIndex] = value;
				else throw new IndexOutOfRangeException("You tried to set this matrix at: (" + rowIndex + ", " + columnIndex + ")");
			}
		}

		public float Determinant
		{
			get
			{
				return
					Row0.X * Row1.Y * Row2.Z * Row3.W - Row0.X * Row1.Y * Row2.W * Row3.Z + Row0.X * Row1.Z * Row2.W * Row3.Y - Row0.X * Row1.Z * Row2.Y * Row3.W
				  + Row0.X * Row1.W * Row2.Y * Row3.Z - Row0.X * Row1.W * Row2.Z * Row3.Y - Row0.Y * Row1.Z * Row2.W * Row3.X + Row0.Y * Row1.Z * Row2.X * Row3.W
				  - Row0.Y * Row1.W * Row2.X * Row3.Z + Row0.Y * Row1.W * Row2.Z * Row3.X - Row0.Y * Row1.X * Row2.Z * Row3.W + Row0.Y * Row1.X * Row2.W * Row3.Z
				  + Row0.Z * Row1.W * Row2.X * Row3.Y - Row0.Z * Row1.W * Row2.Y * Row3.X + Row0.Z * Row1.X * Row2.Y * Row3.W - Row0.Z * Row1.X * Row2.W * Row3.Y
				  + Row0.Z * Row1.Y * Row2.W * Row3.X - Row0.Z * Row1.Y * Row2.X * Row3.W - Row0.W * Row1.X * Row2.Y * Row3.Z + Row0.W * Row1.X * Row2.Z * Row3.Y
				  - Row0.W * Row1.Y * Row2.Z * Row3.X + Row0.W * Row1.Y * Row2.X * Row3.Z - Row0.W * Row1.Z * Row2.X * Row3.Y + Row0.W * Row1.Z * Row2.Y * Row3.X;
			}
		}

		public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix4 result)
		{
			float cos = (float)System.Math.Cos(-angle);
			float sin = (float)System.Math.Sin(-angle);
			float t = 1.0f - cos;

			axis = axis.Normalize();

			result = new Matrix4(t * axis.X * axis.X + cos, t * axis.X * axis.Y - sin * axis.Z, t * axis.X * axis.Z + sin * axis.Y, 0.0f,
								 t * axis.X * axis.Y + sin * axis.Z, t * axis.Y * axis.Y + cos, t * axis.Y * axis.Z - sin * axis.X, 0.0f,
								 t * axis.X * axis.Z - sin * axis.Y, t * axis.Y * axis.Z + sin * axis.X, t * axis.Z * axis.Z + cos, 0.0f,
								 0, 0, 0, 1);
		}

		public static Matrix4 CreateFromAxisAngle(ref Vector3 axis, float angle)
		{
			Matrix4 result;
			CreateFromAxisAngle(ref axis, angle, out result);
			return result;
		}

		public static void CreateRotationX(float angle, out Matrix4 result)
		{
			float cos = (float)System.Math.Cos(angle);
			float sin = (float)System.Math.Sin(angle);

			result = new Matrix4(
				Vector4.UnitX,
				new Vector4(0.0f, cos, sin, 0.0f),
				new Vector4(0.0f, -sin, cos, 0.0f),
				Vector4.UnitW);
		}

		public static Matrix4 CreateRotationX(float angle)
		{
			Matrix4 result;
			CreateRotationX(angle, out result);
			return result;
		}

		public static void CreateRotationY(float angle, out Matrix4 result)
		{
			float cos = (float)System.Math.Cos(angle);
			float sin = (float)System.Math.Sin(angle);

			result = new Matrix4(
				new Vector4(cos, 0.0f, -sin, 0.0f),
				Vector4.UnitY,
				new Vector4(sin, 0.0f, cos, 0.0f),
				Vector4.UnitW);
		}

		public static Matrix4 CreateRotationY(float angle)
		{
			Matrix4 result;
			CreateRotationY(angle, out result);
			return result;
		}

		public static void CreateRotationZ(float angle, out Matrix4 result)
		{
			float cos = (float)System.Math.Cos(angle);
			float sin = (float)System.Math.Sin(angle);

			result = new Matrix4(
				new Vector4(cos, sin, 0.0f, 0.0f),
				new Vector4(-sin, cos, 0.0f, 0.0f),
				Vector4.UnitZ,
				Vector4.UnitW);
		}

		public static Matrix4 CreateRotationZ(float angle)
		{
			Matrix4 result;
			CreateRotationZ(angle, out result);
			return result;
		}

		public static void CreateTranslation(float x, float y, float z, out Matrix4 result)
		{
			result = new Matrix4(
				Vector4.UnitX,
				Vector4.UnitY,
				Vector4.UnitZ,
				new Vector4(x, y, z, 1));
		}

		public static void CreateTranslation(ref Vector3 vector, out Matrix4 result)
		{
			CreateTranslation(vector.X, vector.Y, vector.Z, out result);
		}

		public static Matrix4 CreateTranslation(float x, float y, float z)
		{
			Matrix4 result;
			CreateTranslation(x, y, z, out result);
			return result;
		}

		public static Matrix4 CreateTranslation(Vector3 vector)
		{
			Matrix4 result;
			CreateTranslation(vector.X, vector.Y, vector.Z, out result);
			return result;
		}

		public static void CreateOrthographic(float width, float height, float zNear, float zFar, out Matrix4 result)
		{
			CreateOrthographicOffCenter(-width / 2, width / 2, -height / 2, height / 2, zNear, zFar, out result);
		}

		public static Matrix4 CreateOrthographic(float width, float height, float zNear, float zFar)
		{
			Matrix4 result;
			CreateOrthographicOffCenter(-width / 2, width / 2, -height / 2, height / 2, zNear, zFar, out result);
			return result;
		}

		public static void CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNear, float zFar, out Matrix4 result)
		{
			result = Identity;

			float invRL = 1.0f / (right - left);
			float invTB = 1.0f / (top - bottom);
			float invFN = 1.0f / (zFar - zNear);

			result.Row0.X = 2 * invRL;
			result.Row1.Y = 2 * invTB;
			result.Row2.Z = -2 * invFN;

			result.Row3.X = -(right + left) * invRL;
			result.Row3.Y = -(top + bottom) * invTB;
			result.Row3.Z = -(zFar + zNear) * invFN;
		}

		public static Matrix4 CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNear, float zFar)
		{
			Matrix4 result;
			CreateOrthographicOffCenter(left, right, bottom, top, zNear, zFar, out result);
			return result;
		}

		public static void CreatePerspectiveFieldOfView(float fovy, float aspect, float zNear, float zFar, out Matrix4 result)
		{
			if (fovy <= 0 || fovy > System.Math.PI)
				throw new ArgumentOutOfRangeException("fovy");
			if (aspect <= 0)
				throw new ArgumentOutOfRangeException("aspect");
			if (zNear <= 0)
				throw new ArgumentOutOfRangeException("zNear");
			if (zFar <= 0)
				throw new ArgumentOutOfRangeException("zFar");

			float yMax = zNear * (float)System.Math.Tan(0.5f * fovy);
			float yMin = -yMax;
			float xMin = yMin * aspect;
			float xMax = yMax * aspect;

			CreatePerspectiveOffCenter(xMin, xMax, yMin, yMax, zNear, zFar, out result);
		}

		public static Matrix4 CreatePerspectiveFieldOfView(float fovy, float aspect, float zNear, float zFar)
		{
			Matrix4 result;
			CreatePerspectiveFieldOfView(fovy, aspect, zNear, zFar, out result);
			return result;
		}

		public static void CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float zNear, float zFar, out Matrix4 result)
		{
			if (zNear <= 0)
				throw new ArgumentOutOfRangeException("zNear");
			if (zFar <= 0)
				throw new ArgumentOutOfRangeException("zFar");
			if (zNear >= zFar)
				throw new ArgumentOutOfRangeException("zNear");

			float x = (2.0f * zNear) / (right - left);
			float y = (2.0f * zNear) / (top - bottom);
			float a = (right + left) / (right - left);
			float b = (top + bottom) / (top - bottom);
			float c = -(zFar + zNear) / (zFar - zNear);
			float d = -(2.0f * zFar * zNear) / (zFar - zNear);

			result = new Matrix4(x, 0, 0, 0,
								 0, y, 0, 0,
								 a, b, c, -1,
								 0, 0, d, 0);
		}

		public static Matrix4 CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float zNear, float zFar)
		{
			Matrix4 result;
			CreatePerspectiveOffCenter(left, right, bottom, top, zNear, zFar, out result);
			return result;
		}

		public static Matrix4 Scale(float scale)
		{
			return Scale(scale, scale, scale);
		}

		public static Matrix4 Scale(Vector3 scale)
		{
			return Scale(scale.X, scale.Y, scale.Z);
		}

		public static Matrix4 Scale(float x, float y, float z)
		{
			return new Matrix4(
				Vector4.UnitX * x,
				Vector4.UnitY * y,
				Vector4.UnitZ * z,
				Vector4.UnitW);
		}

		public static Matrix4 LookAt(Vector3 eye, Vector3 target, Vector3 up)
		{
			Vector3 z = Vector3.Normalize(eye - target);
			Vector3 x = Vector3.Normalize(Vector3.Cross(up, z));
			Vector3 y = Vector3.Normalize(Vector3.Cross(z, x));

			Matrix4 rot = new Matrix4(new Vector4(x.X, y.X, z.X, 0.0f),
										new Vector4(x.Y, y.Y, z.Y, 0.0f),
										new Vector4(x.Z, y.Z, z.Z, 0.0f),
										Vector4.UnitW);

			Matrix4 trans = Matrix4.CreateTranslation(-eye);

			return trans * rot;
		}

		public static Matrix4 LookAt(float eyeX, float eyeY, float eyeZ, float targetX, float targetY, float targetZ, float upX, float upY, float upZ)
		{
			return LookAt(new Vector3(eyeX, eyeY, eyeZ), new Vector3(targetX, targetY, targetZ), new Vector3(upX, upY, upZ));
		}

		public static Matrix4 Mult(Matrix4 left, Matrix4 right)
		{
			Matrix4 result;
			Mult(ref left, ref right, out result);
			return result;
		}

		public static void Mult(ref Matrix4 left, ref Matrix4 right, out Matrix4 result)
		{
			float lM11 = left.Row0.X, lM12 = left.Row0.Y, lM13 = left.Row0.Z, lM14 = left.Row0.W,
				lM21 = left.Row1.X, lM22 = left.Row1.Y, lM23 = left.Row1.Z, lM24 = left.Row1.W,
				lM31 = left.Row2.X, lM32 = left.Row2.Y, lM33 = left.Row2.Z, lM34 = left.Row2.W,
				lM41 = left.Row3.X, lM42 = left.Row3.Y, lM43 = left.Row3.Z, lM44 = left.Row3.W,
				rM11 = right.Row0.X, rM12 = right.Row0.Y, rM13 = right.Row0.Z, rM14 = right.Row0.W,
				rM21 = right.Row1.X, rM22 = right.Row1.Y, rM23 = right.Row1.Z, rM24 = right.Row1.W,
				rM31 = right.Row2.X, rM32 = right.Row2.Y, rM33 = right.Row2.Z, rM34 = right.Row2.W,
				rM41 = right.Row3.X, rM42 = right.Row3.Y, rM43 = right.Row3.Z, rM44 = right.Row3.W;

			result = new Matrix4(
				(((lM11 * rM11) + (lM12 * rM21)) + (lM13 * rM31)) + (lM14 * rM41),
				(((lM11 * rM12) + (lM12 * rM22)) + (lM13 * rM32)) + (lM14 * rM42),
				(((lM11 * rM13) + (lM12 * rM23)) + (lM13 * rM33)) + (lM14 * rM43),
				(((lM11 * rM14) + (lM12 * rM24)) + (lM13 * rM34)) + (lM14 * rM44),
				(((lM21 * rM11) + (lM22 * rM21)) + (lM23 * rM31)) + (lM24 * rM41),
				(((lM21 * rM12) + (lM22 * rM22)) + (lM23 * rM32)) + (lM24 * rM42),
				(((lM21 * rM13) + (lM22 * rM23)) + (lM23 * rM33)) + (lM24 * rM43),
				(((lM21 * rM14) + (lM22 * rM24)) + (lM23 * rM34)) + (lM24 * rM44),
				(((lM31 * rM11) + (lM32 * rM21)) + (lM33 * rM31)) + (lM34 * rM41),
				(((lM31 * rM12) + (lM32 * rM22)) + (lM33 * rM32)) + (lM34 * rM42),
				(((lM31 * rM13) + (lM32 * rM23)) + (lM33 * rM33)) + (lM34 * rM43),
				(((lM31 * rM14) + (lM32 * rM24)) + (lM33 * rM34)) + (lM34 * rM44),
				(((lM41 * rM11) + (lM42 * rM21)) + (lM43 * rM31)) + (lM44 * rM41),
				(((lM41 * rM12) + (lM42 * rM22)) + (lM43 * rM32)) + (lM44 * rM42),
				(((lM41 * rM13) + (lM42 * rM23)) + (lM43 * rM33)) + (lM44 * rM43),
				(((lM41 * rM14) + (lM42 * rM24)) + (lM43 * rM34)) + (lM44 * rM44));
		}

		public static Matrix4 operator *(Matrix4 left, Matrix4 right)
		{
			return Matrix4.Mult(left, right);
		}

		public void Invert()
		{
			this = Matrix4.Invert(this);
		}

		public static Matrix4 Invert(Matrix4 mat)
		{
			int[] colIdx = { 0, 0, 0, 0 };
			int[] rowIdx = { 0, 0, 0, 0 };
			int[] pivotIdx = { -1, -1, -1, -1 };

			// convert the matrix to an array for easy looping
			float[,] inverse = {{mat.Row0.X, mat.Row0.Y, mat.Row0.Z, mat.Row0.W}, 
                                {mat.Row1.X, mat.Row1.Y, mat.Row1.Z, mat.Row1.W}, 
                                {mat.Row2.X, mat.Row2.Y, mat.Row2.Z, mat.Row2.W}, 
                                {mat.Row3.X, mat.Row3.Y, mat.Row3.Z, mat.Row3.W} };
			int icol = 0;
			int irow = 0;
			for (int i = 0; i < 4; i++)
			{
				// Find the largest pivot value
				float maxPivot = 0.0f;
				for (int j = 0; j < 4; j++)
				{
					if (pivotIdx[j] != 0)
					{
						for (int k = 0; k < 4; ++k)
						{
							if (pivotIdx[k] == -1)
							{
								float absVal = System.Math.Abs(inverse[j, k]);
								if (absVal > maxPivot)
								{
									maxPivot = absVal;
									irow = j;
									icol = k;
								}
							}
							else if (pivotIdx[k] > 0)
							{
								return mat;
							}
						}
					}
				}

				++(pivotIdx[icol]);

				// Swap rows over so pivot is on diagonal
				if (irow != icol)
				{
					for (int k = 0; k < 4; ++k)
					{
						float f = inverse[irow, k];
						inverse[irow, k] = inverse[icol, k];
						inverse[icol, k] = f;
					}
				}

				rowIdx[i] = irow;
				colIdx[i] = icol;

				float pivot = inverse[icol, icol];
				// check for singular matrix
				if (pivot == 0.0f)
				{
					throw new InvalidOperationException("Matrix is singular and cannot be inverted.");
					//return mat;
				}

				// Scale row so it has a unit diagonal
				float oneOverPivot = 1.0f / pivot;
				inverse[icol, icol] = 1.0f;
				for (int k = 0; k < 4; ++k)
					inverse[icol, k] *= oneOverPivot;

				// Do elimination of non-diagonal elements
				for (int j = 0; j < 4; ++j)
				{
					// check this isn't on the diagonal
					if (icol != j)
					{
						float f = inverse[j, icol];
						inverse[j, icol] = 0.0f;
						for (int k = 0; k < 4; ++k)
							inverse[j, k] -= inverse[icol, k] * f;
					}
				}
			}

			for (int j = 3; j >= 0; --j)
			{
				int ir = rowIdx[j];
				int ic = colIdx[j];
				for (int k = 0; k < 4; ++k)
				{
					float f = inverse[k, ir];
					inverse[k, ir] = inverse[k, ic];
					inverse[k, ic] = f;
				}
			}

			mat.Row0 = new Vector4(inverse[0, 0], inverse[0, 1], inverse[0, 2], inverse[0, 3]);
			mat.Row1 = new Vector4(inverse[1, 0], inverse[1, 1], inverse[1, 2], inverse[1, 3]);
			mat.Row2 = new Vector4(inverse[2, 0], inverse[2, 1], inverse[2, 2], inverse[2, 3]);
			mat.Row3 = new Vector4(inverse[3, 0], inverse[3, 1], inverse[3, 2], inverse[3, 3]);
			return mat;
		}

		public override string ToString()
		{
			return String.Format("{0}\n{1}\n{2}\n{3}", Row0, Row1, Row2, Row3);
		}

		/// <summary>
		/// The first column of this matrix
		/// </summary>
		public Vector4 Column0
		{
			get { return new Vector4(Row0.X, Row1.X, Row2.X, Row3.X); }
		}

		/// <summary>
		/// The second column of this matrix
		/// </summary>
		public Vector4 Column1
		{
			get { return new Vector4(Row0.Y, Row1.Y, Row2.Y, Row3.Y); }
		}

		/// <summary>
		/// The third column of this matrix
		/// </summary>
		public Vector4 Column2
		{
			get { return new Vector4(Row0.Z, Row1.Z, Row2.Z, Row3.Z); }
		}

		/// <summary>
		/// The fourth column of this matrix
		/// </summary>
		public Vector4 Column3
		{
			get { return new Vector4(Row0.W, Row1.W, Row2.W, Row3.W); }
		}

		/// <summary>
		/// Build a rotation matrix from a quaternion
		/// </summary>
		/// <param name="q">the quaternion</param>
		/// <returns>A rotation matrix</returns>
		public static Matrix4 Rotate(Quaternion q)
		{
			Vector3 axis;
			float angle;
			q.ToAxisAngle(out axis, out angle);
			return CreateFromAxisAngle(ref axis, angle);
		}

		public static void Rotate(ref Quaternion q, out Matrix4 m)
		{
			Vector3 axis;
			float angle;
			q.ToAxisAngle(out axis, out angle);

			CreateFromAxisAngle(ref axis, angle, out m);
		}

		public static Matrix4 Transpose(Matrix4 mat)
		{
			return new Matrix4(mat.Column0, mat.Column1, mat.Column2, mat.Column3);
		}

		public static bool operator ==(Matrix4 left, Matrix4 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Matrix4 left, Matrix4 right)
		{
			return !left.Equals(right);
		}

		public override int GetHashCode()
		{
			return Row0.GetHashCode() ^ Row1.GetHashCode() ^ Row2.GetHashCode() ^ Row3.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Matrix4))
				return false;

			return this.Equals((Matrix4)obj);
		}

		public bool Equals(Matrix4 other)
		{
			return
				Row0 == other.Row0 &&
				Row1 == other.Row1 &&
				Row2 == other.Row2 &&
				Row3 == other.Row3;
		}

		public float M11 { get { return Row0.X; } set { Row0.X = value; } }
		public float M12 { get { return Row0.Y; } set { Row0.Y = value; } }
		public float M13 { get { return Row0.Z; } set { Row0.Z = value; } }
		public float M14 { get { return Row0.W; } set { Row0.W = value; } }
		public float M21 { get { return Row1.X; } set { Row1.X = value; } }
		public float M22 { get { return Row1.Y; } set { Row1.Y = value; } }
		public float M23 { get { return Row1.Z; } set { Row1.Z = value; } }
		public float M24 { get { return Row1.W; } set { Row1.W = value; } }
		public float M31 { get { return Row2.X; } set { Row2.X = value; } }
		public float M32 { get { return Row2.Y; } set { Row2.Y = value; } }
		public float M33 { get { return Row2.Z; } set { Row2.Z = value; } }
		public float M34 { get { return Row2.W; } set { Row2.W = value; } }
		public float M41 { get { return Row3.X; } set { Row3.X = value; } }
		public float M42 { get { return Row3.Y; } set { Row3.Y = value; } }
		public float M43 { get { return Row3.Z; } set { Row3.Z = value; } }
		public float M44 { get { return Row3.W; } set { Row3.W = value; } }
	}
}
