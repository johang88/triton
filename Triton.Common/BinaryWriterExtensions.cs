using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Triton.Common
{
	public static class BinaryWriterExtensions
	{
		public static void Write(this BinaryWriter writer, ref Vector2 v)
		{
			writer.Write(v.X);
			writer.Write(v.Y);
		}

		public static void Write(this BinaryWriter writer, Vector2 v)
		{
			Write(writer, ref v);
		}

		public static void Write(this BinaryWriter writer, ref Vector3 v)
		{
			writer.Write(v.X);
			writer.Write(v.Y);
			writer.Write(v.Z);
		}

		public static void Write(this BinaryWriter writer, Vector3 v)
		{
			Write(writer, ref v);
		}

		public static void Write(this BinaryWriter writer, ref Vector4 v)
		{
			writer.Write(v.X);
			writer.Write(v.Y);
			writer.Write(v.Z);
			writer.Write(v.W);
		}

		public static void Write(this BinaryWriter writer, Vector4 v)
		{
			Write(writer, ref v);
		}

		public static void Write(this BinaryWriter writer, ref Quaternion v)
		{
			writer.Write(v.X);
			writer.Write(v.Y);
			writer.Write(v.Z);
			writer.Write(v.W);
		}

		public static void Write(this BinaryWriter writer, Quaternion v)
		{
			Write(writer, ref v);
		}

		public static void Write(this BinaryWriter writer, ref Matrix4 m)
		{
			Write(writer, ref m.Row0);
			Write(writer, ref m.Row1);
			Write(writer, ref m.Row2);
			Write(writer, ref m.Row3);
		}

		public static void Write(this BinaryWriter writer, Matrix4 m)
		{
			Write(writer, ref m);
		}
	}
}
