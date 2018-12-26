using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Components
{
    public abstract class BasePhysicsComponent : GameObjectComponent
    {
        protected BulletSharp.CollisionObject _nativeCollisionObject;

        protected World PhysicsWorld => Owner.World.Services.Get<World>();

        private bool _canSleep = false;
        [DataMember]
        public bool CanSleep
        {
            get => _canSleep;
            set
            {
                _canSleep = value;

                if (_nativeCollisionObject != null)
                    _nativeCollisionObject.ActivationState = _canSleep ? BulletSharp.ActivationState.ActiveTag : BulletSharp.ActivationState.DisableDeactivation;
            }
        }

        public bool IsActive => _nativeCollisionObject?.IsActive ?? false;

        private float _restitution;
        [DataMember]
        public float Restitution
        {
            get => _restitution;
            set
            {
                _restitution = value;

                if (_nativeCollisionObject != null)
                    _nativeCollisionObject.Restitution = _restitution;
            }
        }

        private float _friction = 0.5f;
        [DataMember]
        public float Friction
        {
            get => _friction;
            set
            {
                _friction = value;

                if (_nativeCollisionObject != null)
                    _nativeCollisionObject.Friction = _friction;
            }
        }

        private float _rollingFriction = 0.0f;
        [DataMember]
        public float RollingFriction
        {
            get => _rollingFriction;
            set
            {
                _rollingFriction = value;

                if (_nativeCollisionObject != null)
                    _nativeCollisionObject.RollingFriction = _rollingFriction;
            }
        }

        [DataMember] public IColliderShape ColliderShape { get; set; }

        private CollisionShape _nativeCollisionShape;
        protected CollisionShape NativeCollisionShape
        {
            get
            {
                if (_nativeCollisionShape == null)
                {
                    _nativeCollisionShape = CreateNativeCollisionShape();
                    _nativeCollisionShape.LocalScaling = Conversion.ToBulletVector(ref Owner.Scale);
                }

                return _nativeCollisionShape;
            }
        }

        private CollisionShape CreateNativeCollisionShape()
        {
            switch (ColliderShape)
            {
                case Shapes.SphereColliderShape sphere:
                    return new SphereShape(sphere.Radius);
                case Shapes.BoxColliderShape box:
                    return new BoxShape(box.Width / 2.0f, box.Height / 2.0f, box.Depth / 2.0f);
                case Shapes.CapsuleColliderShape capsule:
                    return new CapsuleShape(capsule.Radius, capsule.Height);
                case Shapes.MeshColliderShape mesh:
                    return mesh.Mesh.Shape;
                default:
                    throw new InvalidOperationException();
            }
        }

        public override void OnActivate()
        {
            base.OnActivate();

            // Apply values to native object
            CanSleep = _canSleep;
            Restitution = _restitution;
            Friction = _friction;
            RollingFriction = _rollingFriction;

            var world = Matrix4.CreateTranslation(Owner.Position) * Matrix4.Rotate(Owner.Orientation);
            _nativeCollisionObject.WorldTransform = Conversion.ToBulletMatrix(ref world);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            if (_nativeCollisionObject != null)
            {
                _nativeCollisionObject.UserObject = null;
                _nativeCollisionObject.Dispose();
                _nativeCollisionObject = null;
            }
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            // We probably dont have to do this all the time
            UpdateTransformFromPhysicsTransform();
        }

        protected void UpdateTransformFromPhysicsTransform()
        {
            if (_nativeCollisionObject == null) return;

            _nativeCollisionObject.WorldTransform.Decompose(out _, out var rotation, out var translation);

            Owner.Position = Conversion.ToTritonVector(ref translation);
            Owner.Orientation = Conversion.ToTritonQuaternion(rotation);
        }
    }
}
