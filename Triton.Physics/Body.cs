using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics
{
	public class Body
	{
		internal Jitter.Dynamics.RigidBody RigidBody;

		public Vector3 Position = Vector3.Zero;
		public Matrix4 Orientation = Matrix4.Identity;

		public object Tag = null;
		public int CollisionLayer = 1;

		internal Body(Jitter.Dynamics.RigidBody rigidBody)
		{
			RigidBody = rigidBody;
			RigidBody.EnableSpeculativeContacts = true;
		}

		internal virtual void Update()
		{
			Position = Conversion.ToTritonVector(RigidBody.Position);
			Orientation = Conversion.ToTritonMatrix(RigidBody.Orientation);
		}

		public void SetPosition(Vector3 position)
		{
			Position = position;
			RigidBody.Position = Conversion.ToJitterVector(ref position);
		}
	}
}
