using Jitter.Dynamics;
using Jitter.LinearMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics
{
	public class CharacterController : Body
	{
		private readonly Character.CharacterControllerConstraint Controller;

		public Vector3 TargetVelocity = Vector3.Zero;
		public bool TryJump = false;

		internal CharacterController(Jitter.World world, RigidBody body)
			: base(body)
		{
			RigidBody.SetMassProperties(JMatrix.Zero, 1.0f, true);
			RigidBody.AllowDeactivation = false;

			Controller = new Character.CharacterControllerConstraint(world, RigidBody);
			world.AddConstraint(Controller);
		}

		internal override void Update()
		{
			base.Update();

			Controller.TargetVelocity = Conversion.ToJitterVector(ref TargetVelocity);
			Controller.TryJump = TryJump;
		}
	}
}
