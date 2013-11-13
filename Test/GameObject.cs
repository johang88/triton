using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton;

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
			Mesh.World = Body.Orientation * Matrix4.CreateTranslation(Body.Position);
		}
	}
}
