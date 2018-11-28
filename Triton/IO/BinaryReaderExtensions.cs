using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Triton.IO
{
	public static class BinaryReaderExtensions
	{
		public static void ReadVector2(this BinaryReader reader, ref Vector2 v)
		{
			v.X = reader.ReadSingle();
			v.Y = reader.ReadSingle();
		}

		public static Vector2 ReadVector2(this BinaryReader reader)
		{
			Vector2 v = new Vector2();
			ReadVector2(reader, ref v);
			return v;
		}

		public static void ReadVector3(this BinaryReader reader, ref Vector3 v)
		{
			v.X = reader.ReadSingle();
			v.Y = reader.ReadSingle();
			v.Z = reader.ReadSingle();
		}

		public static Vector3 ReadVector3(this BinaryReader reader)
		{
			Vector3 v = new Vector3();
			ReadVector3(reader, ref v);
			return v;
		}

		public static void ReadVector4(this BinaryReader reader, ref Vector4 v)
		{
			v.X = reader.ReadSingle();
			v.Y = reader.ReadSingle();
			v.Z = reader.ReadSingle();
			v.W = reader.ReadSingle();
		}

		public static Vector4 ReadVector4(this BinaryReader reader)
		{
			Vector4 v = new Vector4();
			ReadVector4(reader, ref v);
			return v;
		}

		public static void ReadQuaternion(this BinaryReader reader, ref Quaternion v)
		{
			v.X = reader.ReadSingle();
			v.Y = reader.ReadSingle();
			v.Z = reader.ReadSingle();
			v.W = reader.ReadSingle();
		}

		public static Quaternion ReadQuaternion(this BinaryReader reader)
		{
			Quaternion v = new Quaternion();
			ReadQuaternion(reader, ref v);
			return v;
		}

		public static void ReadMatrix4(this BinaryReader reader, ref Matrix4 matrix)
		{
			ReadVector4(reader, ref matrix.Row0);
			ReadVector4(reader, ref matrix.Row1);
			ReadVector4(reader, ref matrix.Row2);
			ReadVector4(reader, ref matrix.Row3);
		}

		public static Matrix4 ReadMatrix4(this BinaryReader reader)
		{
			Matrix4 m = new Matrix4();
			ReadMatrix4(reader, ref m);
			return m;
		}
	}
}
