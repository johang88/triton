using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Samples
{
	class RotationComponent : Game.World.Component
	{
		public Vector3 Rotation = new Vector3();

		public override void Update(float stepSize)
		{
			base.Update(stepSize);

			var rot = Rotation * stepSize;

			Owner.Orientation *=
				Matrix4.CreateRotationX(rot.X)
				* Matrix4.CreateRotationY(rot.Y)
				* Matrix4.CreateRotationZ(rot.Z);
		}
	}
}
