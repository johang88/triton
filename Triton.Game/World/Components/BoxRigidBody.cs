using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Physics;

namespace Triton.Game.World.Components
{
	public class BoxRigidBody : RigidBody
	{
        [DataMember] public float Length = 1;
        [DataMember] public float Height = 1;
        [DataMember] public float Width = 1;

        protected override Body CreateBody(BodyFlags flags)
            => World.PhysicsWorld.CreateBoxBody(Length, Height, Width, Owner.Position, Mass, flags);
	}
}
