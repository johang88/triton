using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
	class GameObject
	{
		public readonly Triton.Graphics.MeshInstance Mesh;
		public readonly Triton.Physics.Body Body;

		public GameObject(Triton.Graphics.MeshInstance mesh, Triton.Physics.Body body)
		{
			Mesh = mesh;
			Body = body;
		}

		public void Update()
		{
			Mesh.Position = Body.Position;

			var orientationMatrix = Body.Orientation;
			Mesh.Orientation = new Triton.Quaternion(ref orientationMatrix);
		}
	}
}
