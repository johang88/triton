using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class Transform : Component
	{
		public Vector3 Position = Vector3.Zero;
		public Quaternion Orientation = Quaternion.Identity;

		public Vector3 LocalPosition = Vector3.Zero;
		public Quaternion LocalOrientation = Quaternion.Identity;

		public override void OnActivate()
		{
			base.OnActivate();

			UpdateChildTransforms();
		}

		public override void Update(float stepSize)
		{
			base.Update(stepSize);

			if (Parent == null)
			{
				LocalPosition = Position;
				LocalOrientation = Orientation;
			}

			UpdateChildTransforms();
		}

		public void UpdateChildTransforms()
		{
			Vector3 positionInParentSpace;
			foreach (var child in Owner.GetChildren())
			{
				Quaternion.Multiply(ref Orientation, ref child.Transform.LocalOrientation, out child.Transform.Orientation);

				Vector3.Transform(ref child.Transform.LocalPosition, ref Orientation, out positionInParentSpace);
				Vector3.Add(ref Position, ref positionInParentSpace, out child.Transform.Position);
			}
		}
	}
}
