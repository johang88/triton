using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class SphereRigidBody : RigidBody
	{
        [DataMember] public float Radius { get; set; } = 1.0f;
        [DataMember] public bool IsStatic { get; set; } = false;
        [DataMember] public float Mass { get; set; } = 1.0f;

		public override void OnActivate()
		{
			base.OnActivate();

			Body = World.PhysicsWorld.CreateSphereBody(Radius, Owner.Position, IsStatic, Mass);
		}
	}
}
