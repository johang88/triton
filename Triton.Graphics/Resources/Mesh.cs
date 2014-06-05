using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Mesh : Triton.Common.Resource
	{
		public SubMesh[] SubMeshes { get; internal set; }

		public Mesh(string name, string parameters)
			: base(name, parameters)
		{
			SubMeshes = new SubMesh[0];
		}
	}

	public class SubMesh
	{
		public Material Material;
		public float BoundingSphereRadius;
		public int Handle;
	}
}
