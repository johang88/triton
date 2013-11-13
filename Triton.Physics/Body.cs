using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics
{
	public class Body
	{
		public readonly int Id;
		internal Jitter.Dynamics.RigidBody RigidBody;

		public Vector3 Position;
		public Matrix4 Orientation;

		public object Tag = null;
		public int CollisionLayer = 1;

		internal Body(Jitter.Dynamics.RigidBody rigidBody, int id)
		{
			RigidBody = rigidBody;
			Id = id;
		}

		internal void Update()
		{
			Position = Conversion.ToTritonVector(RigidBody.Position);
			Orientation = Conversion.ToTritonMatrix(RigidBody.Orientation);
		}
	}
}
