using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Physics;

namespace Triton.Game.World.Components
{
	public class CharacterController : Component
	{
        public event BodyCollisionCallback Collision;

        public float Length = 1.5f;
		public float Radius = 0.15f;
        public float WalkSpeed = 5.0f;

        private Physics.CharacterController Controller;

        public override void OnActivate()
        {
            base.OnActivate();

            Controller = World.PhysicsWorld.CreateCharacterController(Length, Radius);
            Controller.Tag = Owner;

            Controller.SetPosition(Owner.Position);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            if (Controller != null)
            {
                Controller.Collision -= OnCollision;
                World.PhysicsWorld.RemoveBody(Controller);
            }
        }

        private void OnCollision(Body other)
        {
            if (other.Tag is GameObject otherGameObject)
            {
                Collision?.Invoke(otherGameObject);
            }
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            if (Controller != null)
            {
                Owner.Position = Controller.Position;
                Owner.Orientation = Controller.Orientation;
            }
        }

		public void Move(Vector3 movement, bool jump)
		{
			Controller.TargetVelocity = movement * WalkSpeed;
			Controller.TryJump = jump;
		}
    }
}
