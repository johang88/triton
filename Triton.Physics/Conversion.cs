using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jitter.LinearMath;

namespace Triton.Physics
{
	static class Conversion
	{
		public static Vector3 ToTritonVector(ref JVector vector)
		{
			return new Vector3(vector.X, vector.Y, vector.Z);
		}

		public static Vector3 ToTritonVector(JVector vector)
		{
			return new Vector3(vector.X, vector.Y, vector.Z);
		}

		public static JVector ToJitterVector(ref Vector3 vector)
		{
			return new JVector(vector.X, vector.Y, vector.Z);
		}

		public static JMatrix ToJitterMatrix(ref Matrix4 matrix)
		{
			return new JMatrix(matrix.M11,
							   matrix.M12,
							   matrix.M13,
								matrix.M21,
								matrix.M22,
								matrix.M23,
								matrix.M31,
								matrix.M32,
								matrix.M33
								);
		}

		public static Matrix4 ToTritonMatrix(ref JMatrix matrix)
		{
			return new Matrix4(matrix.M11,
							   matrix.M12,
							   matrix.M13,
							   0.0f,
							matrix.M21,
							matrix.M22,
							matrix.M23,
							0.0f,
							matrix.M31,
							matrix.M32,
							matrix.M33,
							0.0f, 0.0f, 0.0f, 0.0f, 1.0f);
		}

		public static Matrix4 ToTritonMatrix(JMatrix matrix)
		{
			return new Matrix4(matrix.M11,
							   matrix.M12,
							   matrix.M13,
							   0.0f,
							matrix.M21,
							matrix.M22,
							matrix.M23,
							0.0f,
							matrix.M31,
							matrix.M32,
							matrix.M33,
							0.0f, 0.0f, 0.0f, 0.0f, 1.0f);
		}
	}
}
