using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Physics;

namespace Triton.Game.World.Components
{
	public class SphereRigidBody : RigidBody
	{
        [DataMember] public float Radius { get; set; } = 1.0f;

        protected override Body CreateBody(BodyFlags flags)
            => World.PhysicsWorld.CreateSphereBody(Radius, Owner.Position, Mass, flags);
	}
}
