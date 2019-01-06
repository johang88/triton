using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Components
{
    public class RigidBodyComponent : BasePhysicsComponent
    {
        private BulletSharp.RigidBody _nativeRigidBody = null;

        private float _mass = 1.0f;
        [DataMember]
        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;

                if (_nativeRigidBody != null)
                {
                    var inertia = NativeCollisionShape.CalculateLocalInertia(_mass);
                    _nativeRigidBody.SetMassProps(_mass, inertia);
                }
            }
        }

        private RigidBodyType _rigidBodyType;
        [DataMember]
        public RigidBodyType RigidBodyType
        {
            get => _rigidBodyType;
            set
            {
                _rigidBodyType = value;

                if (_nativeRigidBody != null)
                {
                    switch (_rigidBodyType)
                    {
                        case RigidBodyType.Dynamic:
                            _nativeCollisionObject.CollisionFlags = CollisionFlags.None;
                            break;
                        case RigidBodyType.Kinematic:
                            _nativeCollisionObject.CollisionFlags = CollisionFlags.KinematicObject;
                            break;
                        case RigidBodyType.Static:
                            _nativeCollisionObject.CollisionFlags = CollisionFlags.StaticObject;
                            break;
                    }
                }
            }
        }

        public override void OnActivate()
        {
            var motionState = new DefaultMotionState(BulletSharp.Math.Matrix.Identity);

            NativeCollisionShape.CalculateLocalInertia(_mass, out var localInertia);

            // TODO: This is a little hacky
            if (ColliderShape is Shapes.TerrainColliderShape terrainShape)
            {
                var terrainSize = terrainShape.TerrainData.Size * terrainShape.TerrainData.MetersPerHeightfieldTexel;
                var terrainHalfSize = terrainSize / 2.0f;

                SynchronizePosition = false;

                motionState = new DefaultMotionState(BulletSharp.Math.Matrix.Translation(terrainHalfSize, terrainShape.TerrainData.MaxHeight * 0.5f, terrainHalfSize));
            }

            _nativeRigidBody = new RigidBody(new RigidBodyConstructionInfo(_mass, motionState, NativeCollisionShape, localInertia));
            _nativeRigidBody.UserObject = this;
            
            _nativeCollisionObject = _nativeRigidBody;

            PhysicsWorld.DiscreteDynamicsWorld.AddRigidBody(_nativeRigidBody);

            Mass = _mass;
            RigidBodyType = _rigidBodyType;

            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            if (_nativeRigidBody != null)
            {
                PhysicsWorld.DiscreteDynamicsWorld.RemoveRigidBody(_nativeRigidBody);
                _nativeRigidBody = null;

                // base.OnDeactivate will Dispose
            }

            base.OnDeactivate();
        }

        public void AddForce(Vector3 force)
        {
            _nativeRigidBody.Activate();
            _nativeRigidBody.ApplyCentralImpulse(Conversion.ToBulletVector(ref force));
        }
    }
}
