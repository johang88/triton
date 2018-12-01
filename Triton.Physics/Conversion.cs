using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics
{
	static class Conversion
	{
		public static Vector3 ToTritonVector(ref BulletSharp.Math.Vector3 vector)
		{
			return new Vector3(vector.X, vector.Y, vector.Z);
		}

		public static Vector3 ToTritonVector(BulletSharp.Math.Vector3 vector)
		{
			return new Vector3(vector.X, vector.Y, vector.Z);
		}

		public static BulletSharp.Math.Vector3 ToBulletVector(ref Vector3 vector)
		{
			return new BulletSharp.Math.Vector3(vector.X, vector.Y, vector.Z);
		}

		public static BulletSharp.Math.Matrix ToBulletMatrix(ref Matrix4 matrix)
		{
            return new BulletSharp.Math.Matrix(matrix.M11,
							   matrix.M12,
							   matrix.M13,
							   matrix.M14,
                                matrix.M21,
								matrix.M22,
								matrix.M23,
								matrix.M24,
								matrix.M31,
								matrix.M32,
								matrix.M33,
								matrix.M34,
                                matrix.M41,
                                matrix.M42,
                                matrix.M43,
                                matrix.M44
                                );
		}

		public static Matrix4 ToTritonMatrix(ref BulletSharp.Math.Matrix matrix)
		{
			return new Matrix4(
				matrix.M11, matrix.M12, matrix.M13, matrix.M14,
				matrix.M21, matrix.M22, matrix.M23, matrix.M23,
				matrix.M31, matrix.M32, matrix.M33, matrix.M33,
				matrix.M41, matrix.M42, matrix.M43, matrix.M43);
		}

		public static Matrix4 ToTritonMatrix(BulletSharp.Math.Matrix matrix)
		{
            return new Matrix4(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M23,
                matrix.M31, matrix.M32, matrix.M33, matrix.M33,
                matrix.M41, matrix.M42, matrix.M43, matrix.M43);
        }

        public static BulletSharp.Math.Quaternion ToBulletQuaternion(Quaternion q)
        {
            return new BulletSharp.Math.Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static Quaternion ToTritonQuaternion(BulletSharp.Math.Quaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static Quaternion ToTritonQuaternion(ref BulletSharp.Math.Quaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }
    }
}
