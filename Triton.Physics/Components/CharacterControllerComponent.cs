using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Components
{
    public class CharacterControllerComponent : BasePhysicsComponent
    {
        private KinematicCharacterController _kinematicCharacterController;

        private Vector3 _targetVelocity = Vector3.Zero;

        private float _fallSpeed = 10.0f;
        [DataMember]
        public float FallSpeed
        {
            get => _fallSpeed;
            set
            {
                _fallSpeed = value;
                if (_kinematicCharacterController != null)
                {
                    _kinematicCharacterController.SetFallSpeed(_fallSpeed);
                }
            }
        }

        private float _maxSlope = 0.785398163f;
        public float MaxSlope
        {
            get => _maxSlope;
            set
            {
                _maxSlope = value;
                if (_kinematicCharacterController != null)
                {
                    _kinematicCharacterController.MaxSlope = _maxSlope;
                }
            }
        }

        private float _jumpSpeed = 10.0f;
        public float JumpSpeed
        {
            get => _jumpSpeed;
            set
            {
                _jumpSpeed = value;
                if (_kinematicCharacterController != null)
                {
                    _kinematicCharacterController.SetJumpSpeed(_jumpSpeed);
                }
            }
        }

        private float _gravity = 30.0f;
        public float Gravity
        {
            get => _gravity;
            set
            {
                _gravity = value;
                if (_kinematicCharacterController != null)
                {
                    _kinematicCharacterController.Gravity = _gravity;
                }
            }
        }

        public override void OnActivate()
        {
            _nativeCollisionObject = new PairCachingGhostObject
            {
                CollisionShape = NativeCollisionShape,
                UserObject = this,
                CollisionFlags = CollisionFlags.CharacterObject
            };

            _kinematicCharacterController = new KinematicCharacterController((PairCachingGhostObject)_nativeCollisionObject, (ConvexShape)NativeCollisionShape, 0.1f);

            PhysicsWorld.DiscreteDynamicsWorld.Broadphase.OverlappingPairCache.SetInternalGhostPairCallback(new GhostPairCallback());
            PhysicsWorld.DiscreteDynamicsWorld.AddCollisionObject(_nativeCollisionObject, CollisionFilterGroups.CharacterFilter, CollisionFilterGroups.StaticFilter | CollisionFilterGroups.DefaultFilter);
            PhysicsWorld.DiscreteDynamicsWorld.AddAction(_kinematicCharacterController);

            base.OnActivate();

            FallSpeed = _fallSpeed;
            MaxSlope = _maxSlope;
            JumpSpeed = _jumpSpeed;
            Gravity = _gravity;
        }

        public override void OnDeactivate()
        {
            if (_kinematicCharacterController != null)
            {
                PhysicsWorld.DiscreteDynamicsWorld.RemoveCollisionObject(_nativeCollisionObject);
                PhysicsWorld.DiscreteDynamicsWorld.RemoveAction(_kinematicCharacterController);

                _kinematicCharacterController = null;
            }

            base.OnDeactivate();
        }

        public void SetTargetVelocity(Vector3 targetVelocity)
        {
            _targetVelocity = targetVelocity;
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            _kinematicCharacterController.SetWalkDirection(Conversion.ToBulletVector(ref _targetVelocity) * dt);
        }

        public void Jump()
        {
            _kinematicCharacterController.Jump();
        }
    }
}
