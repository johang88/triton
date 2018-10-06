using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics
{
	public class Body : IDisposable
	{
		internal RigidBody RigidBody;

		public Vector3 Position = Vector3.Zero;
		public Quaternion Orientation = Quaternion.Identity;

		public object Tag = null;
		public int CollisionLayer = 1;

		internal Body(RigidBody rigidBody)
		{
			RigidBody = rigidBody;
		}

		internal virtual void Update(float dt)
		{
            if (RigidBody == null) return;

            var bulletWorldMatrix = RigidBody.MotionState.WorldTransform;
            var world = Conversion.ToTritonMatrix(ref bulletWorldMatrix);

            Position = Conversion.ToTritonVector(bulletWorldMatrix.Origin);
            Orientation = Conversion.ToTritonQuaternion(RigidBody.Orientation);
        }

		public virtual void SetPosition(Vector3 position)
		{
            if (RigidBody == null) return;

			Position = position;

            var world = Matrix4.Rotate(Conversion.ToTritonQuaternion(RigidBody.Orientation)) * Matrix4.CreateTranslation(position);
            RigidBody.WorldTransform = Conversion.ToBulletMatrix(ref world);
		}

		public void AddForce(Vector3 force)
		{
            RigidBody.Activate();
            RigidBody.ApplyCentralImpulse(Conversion.ToBulletVector(ref force));
        }

        public virtual void Dispose()
        {
        }
    }
}
