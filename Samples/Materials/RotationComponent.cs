using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Game.World.Components;

namespace Triton.Samples
{
	class RotationComponent : BaseComponent
	{
		public Vector3 Rotation = new Vector3();

		public override void Update(float dt)
		{
			base.Update(dt);

			var rot = Rotation * dt;

            var m = Matrix4.CreateRotationX(rot.X)
                * Matrix4.CreateRotationY(rot.Y)
                * Matrix4.CreateRotationZ(rot.Z);

            Owner.Orientation *= new Quaternion(ref m);
		}
	}
}
