﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public class MeshInstance
	{
		public Resources.Mesh Mesh;

		public Vector3 Position;
		public Quaternion Orientation = Quaternion.Identity;
	}
}
