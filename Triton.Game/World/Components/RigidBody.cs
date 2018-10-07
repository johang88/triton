using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class RigidBody : Component
	{
		public Physics.Body Body;

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            if (Body != null)
            {
                World.PhysicsWorld.RemoveBody(Body);
            }
        }

        public override void Update(float dt)
		{
			base.Update(dt);

            if (Body != null)
            {
                Owner.Position = Body.Position;
                Owner.Orientation = Body.Orientation;
            }
		}

        public void AddForce(Vector3 force)
		{
			Body.AddForce(force);
		}
	}
}
