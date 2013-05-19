﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Triton
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Matrix4
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

		public static void CreateFromAxisAngle(Vector3 axis, float angle, out Matrix4 result)
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

		public static Matrix4 CreateFromAxisAngle(Vector3 axis, float angle)
		{
			Matrix4 result;
			CreateFromAxisAngle(axis, angle, out result);
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
			float invRL = 1 / (right - left);
			float invTB = 1 / (top - bottom);
			float invFN = 1 / (zFar - zNear);

			result = new Matrix4(
				2 * invRL, 2 * invTB, -2 * invFN, 0,
				0, 0, 0, 0,
				0, 0, 0, 0,
				(right + left) * invRL, -(top + bottom) * invTB, -(zFar + zNear) * invFN, 1);
		}

		public static Matrix4 CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNear, float zFar)
		{
			Matrix4 result;
			CreateOrthographicOffCenter(left, right, bottom, top, zNear, zFar, out result);
			return result;
		}

		public static void CreatePerspectiveFieldOfView(float fovy, float aspect, float zNear, float zFar, out Matrix4 result)
		{
			if (fovy <= 0 || fovy > Math.PI)
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

		public override string ToString()
		{
			return String.Format("{0}\n{1}\n{2}\n{3}", Row0, Row1, Row2, Row3);
		}
	}
}