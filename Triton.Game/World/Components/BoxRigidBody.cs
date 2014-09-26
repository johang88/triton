using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class BoxRigidBody : RigidBody
	{
		public float Length = 1;
		public float Height = 1;
		public float Width = 1;
		public bool IsStatic = false;

		public override void OnAttached(GameObject owner)
		{
			base.OnAttached(owner);

			Body = World.PhysicsWorld.CreateBoxBody(Length, Height, Width, Owner.Position, IsStatic);
		}
	}
}
