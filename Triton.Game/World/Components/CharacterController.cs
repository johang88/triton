﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class CharacterController : RigidBody
	{
		public float Length = 1.5f;
		public float Radius = 0.5f;

		private Physics.CharacterController Controller;

		public override void OnAttached(GameObject owner)
		{
			base.OnAttached(owner);

			Body = Controller = World.PhysicsWorld.CreateCharacterController(Length, Radius);
			Controller.SetPosition(Owner.Position);
		}

		public void Move(Vector3 movement, bool jump)
		{
			Controller.TargetVelocity = movement;
			Controller.TryJump = jump;
		}
	}
}