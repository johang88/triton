﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Math
{
	public struct Vector4
	{
		public readonly float X;
		public readonly float Y;
		public readonly float Z;
		public readonly float W;

		public Vector4(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}
	}
}
