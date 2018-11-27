using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class RigidBody : Component
	{
		public Physics.Body Body;
        [DataMember] public int CollisionLayer = 1;

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
                Body.CollisionLayer = CollisionLayer;
            }
		}

        public void AddForce(Vector3 force)
		{
			Body.AddForce(force);
		}
	}
}
