using Jitter.Dynamics;
using Jitter.Dynamics.Constraints;
using Jitter.LinearMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Character
{
	class CharacterControllerConstraint : Constraint
	{
		private const float JumpVelocity = 1.5f;
		private readonly float FeetPosition = 0.0f;
		private readonly Jitter.World World;

		private JVector DeltaVelocity = JVector.Zero;
		public bool ShouldJump = false;
		
		public JVector TargetVelocity = JVector.Zero;
		public bool TryJump = false;
		public RigidBody WalkingOn = null;

		public CharacterControllerConstraint(Jitter.World world, RigidBody body)
			: base(body, null)
		{
			World = world;

			// Determine feet position
			// This is done by supportmapping in the down direction
			// furthest point away from the down direction.
			JVector vec = JVector.Down;
			JVector result = JVector.Zero;

			body.Shape.SupportMapping(ref vec, out result);

			FeetPosition = result * JVector.Down;
		}

		public override void PrepareForIteration(float timestep)
		{
			// Send a ray from feet position down
			// Remember if we collide with something just below our feet

			RigidBody resultBody = null;
			JVector normal;
			float fraction;

			var result = World.CollisionSystem.Raycast(Body1.Position + JVector.Down * (FeetPosition - 0.1f), JVector.Down, RaycastCallback, out resultBody, out normal, out fraction);

			WalkingOn = (result && fraction <= 0.2f) ? resultBody : null;
			ShouldJump = (result && fraction <= 0.2f && Body1.LinearVelocity.Y < JumpVelocity && TryJump);
		}

		private bool RaycastCallback(RigidBody body, JVector normal, float fraction)
		{
			// Prevent collision with self
			return (body != this.Body1);
		}

		public override void Iterate()
		{
			DeltaVelocity = TargetVelocity - Body1.LinearVelocity;
			DeltaVelocity.Y = 0.0f;

			var fraction = 0.02f;
			if (WalkingOn == null)
				fraction = 0.0001f; 
			DeltaVelocity *= fraction;

			if (DeltaVelocity.LengthSquared() != 0.0f)
			{
				Body1.IsActive = true;
				Body1.ApplyImpulse(DeltaVelocity * Body1.Mass);
			}

			if (ShouldJump)
			{
				Body1.IsActive = true;
				Body1.ApplyImpulse(JumpVelocity * JVector.Up * Body1.Mass);

				if (!WalkingOn.IsStatic)
				{
					WalkingOn.IsActive = true;
					WalkingOn.ApplyImpulse(-1.0f * JumpVelocity * JVector.Up * Body1.Mass);
				}
			}
		}
	}
}
