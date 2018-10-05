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

        private Vector3 _previousPosition;
        private Quaternion _previousOrientation;

        private Vector3 _currentPosition;
        private Quaternion _currentOrientation;

        public override void OnDetached()
		{
			base.OnDetached();

			World.PhysicsWorld.RemoveBody(Body);
		}

        public override void OnActivate()
        {
            base.OnActivate();

            _previousPosition = Owner.Position;
            _previousOrientation = Owner.Orientation;
        }

        public override void Update(float dt)
		{
			base.Update(dt);

            Owner.Position = Vector3.Lerp(_previousPosition, _currentPosition, 1);
            Quaternion.Slerp(ref _previousOrientation, ref _currentOrientation, 1, out Owner.Orientation);

			Owner.Position = Body.Position;
            Owner.Orientation = Body.Orientation;
		}

        public override void FixedUpdate(float dt)
        {
            base.FixedUpdate(dt);

            _previousPosition = _currentPosition;
            _previousOrientation = _currentOrientation;

            _currentPosition = Body.Position;
            _currentOrientation = Body.Orientation;
        }

        public void AddForce(Vector3 force)
		{
			Body.AddForce(force);
		}
	}
}
