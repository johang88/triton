using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class SphereRigidBody : RigidBody
	{
		public float Radius = 1;
		public bool IsStatic = false;

		public override void OnAttached(GameObject owner)
		{
			base.OnAttached(owner);

			Body = World.PhysicsWorld.CreateSphereBody(Radius, Owner.Position, IsStatic);
		}
	}
}
