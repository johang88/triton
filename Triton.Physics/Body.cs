using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Physics.Resources;

namespace Triton.Physics
{
    [Flags]
    public enum BodyFlags
    {
        None = 0,
        Static = 1 << 0,
        Kinematic = 1 << 1,
        NoContactResponse = 1 << 2
    }

    public delegate void CollisionCallback(Body other);

    public class Body : IDisposable
    {
        internal RigidBody RigidBody;

        public Vector3 Position = Vector3.Zero;
        public Quaternion Orientation = Quaternion.Identity;

        public object Tag = null;
        public int CollisionLayer = 1;

        public Mesh Mesh;

        BodyFlags _flags = BodyFlags.None;
        public BodyFlags Flags
        {
            get => _flags;
            set
            {
                _flags = value;

                if (_flags.HasFlag(BodyFlags.Static))
                {
                    RigidBody.CollisionFlags |= CollisionFlags.StaticObject;
                }
                else
                {
                    RigidBody.CollisionFlags &= ~CollisionFlags.StaticObject;
                }

                if (_flags.HasFlag(BodyFlags.Kinematic))
                {
                    RigidBody.CollisionFlags |= CollisionFlags.KinematicObject;
                }
                else
                {
                    RigidBody.CollisionFlags &= ~CollisionFlags.KinematicObject;
                }

                if (_flags.HasFlag(BodyFlags.NoContactResponse))
                {
                    RigidBody.CollisionFlags |= CollisionFlags.NoContactResponse;
                }
                else
                {
                    RigidBody.CollisionFlags &= ~CollisionFlags.NoContactResponse;
                }
            }
        }

        public event CollisionCallback Collision;

        internal void OnCollision(Body other)
        {
            Collision?.Invoke(other);
        }

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
