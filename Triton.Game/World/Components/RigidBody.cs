using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Physics;

namespace Triton.Game.World.Components
{
    public delegate void BodyCollisionCallback(GameObject other);

	public abstract class RigidBody : Component
	{
		protected Physics.Body _body;

        [DataMember] public int CollisionLayer = 1;
        [DataMember] public bool IsStatic { get; set; } = false;
        [DataMember] public bool IsTrigger { get; set; } = false;
        [DataMember] public bool IsKinematic { get; set; } = false;
        [DataMember] public float Mass { get; set; } = 1.0f;

        public event BodyCollisionCallback Collision;

        protected abstract Body CreateBody(Physics.BodyFlags flags);

        public override void OnActivate()
        {
            base.OnActivate();

            var flags = Physics.BodyFlags.None;
            if (IsStatic)
            {
                flags |= Physics.BodyFlags.Static;
            }

            if (IsTrigger)
            {
                flags |= Physics.BodyFlags.NoContactResponse;
            }

            if (IsKinematic)
            {
                flags |= Physics.BodyFlags.Kinematic;
            }

            _body = CreateBody(flags);
            _body.Collision += OnCollision;
            _body.Tag = Owner;
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            if (_body != null)
            {
                _body.Collision -= OnCollision;
                World.PhysicsWorld.RemoveBody(_body);
            }
        }

        protected void OnCollision(Body other)
        {
            if (other.Tag is GameObject otherGameObject)
            {
                Collision?.Invoke(otherGameObject);
            }
        }

        public override void Update(float dt)
		{
			base.Update(dt);

            if (_body != null)
            {
                Owner.Position = _body.Position;
                Owner.Orientation = _body.Orientation;
                _body.CollisionLayer = CollisionLayer;
            }
		}

        public void AddForce(Vector3 force)
		{
			_body.AddForce(force);
		}
	}
}
