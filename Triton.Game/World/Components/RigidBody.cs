using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class RigidBody : Component
	{
		protected Physics.Body Body;

		public override void OnDetached()
		{
			base.OnDetached();

			World.PhysicsWorld.RemoveBody(Body);
		}

		public override void Update(float stepSize)
		{
			base.Update(stepSize);

			Owner.Position = Body.Position;
			Owner.Orientation = Body.Orientation;
		}
	}
}
