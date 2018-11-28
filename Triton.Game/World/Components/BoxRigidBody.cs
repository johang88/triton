using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class BoxRigidBody : RigidBody
	{
        [DataMember] public float Length = 1;
        [DataMember] public float Height = 1;
        [DataMember] public float Width = 1;
        [DataMember] public bool IsStatic { get; set; } = false;
        [DataMember] public float Mass { get; set; } = 1.0f;

        public override void OnActivate()
		{
			Body = World.PhysicsWorld.CreateBoxBody(Length, Height, Width, Owner.Position, Mass, IsStatic ? Physics.BodyFlags.Static : Physics.BodyFlags.None);
		}
	}
}
