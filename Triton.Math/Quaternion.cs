using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Triton
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Quaternion
	{
		public float X;
		public float Y;
		public float Z;
		public float W;

		public static readonly Quaternion Identity = new Quaternion(0, 0, 0, 1);

		public Quaternion(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}
	}
}
