using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics
{
    public class CharacterController : Body
    {
        private KinematicCharacterController _kinematicCharacterController;
        private PairCachingGhostObject _ghostObject;
        private ConvexShape _shape;
        private DiscreteDynamicsWorld _world;

        public Vector3 TargetVelocity = Vector3.Zero;
        public bool TryJump = false;

        internal CharacterController(DiscreteDynamicsWorld world, float radius, float height)
            : base(null)
        {
            _world = world;

            _ghostObject = new PairCachingGhostObject();
            _ghostObject.UserObject = this;

            _shape = new CapsuleShape(radius, height);

            _world.Broadphase.OverlappingPairCache.SetInternalGhostPairCallback(new GhostPairCallback());

            _ghostObject.CollisionShape = _shape;
            _ghostObject.CollisionFlags = CollisionFlags.CharacterObject;

            _kinematicCharacterController = new KinematicCharacterController(_ghostObject, _shape, 0.1f);

            _world.AddCollisionObject(_ghostObject, CollisionFilterGroups.CharacterFilter, CollisionFilterGroups.StaticFilter | CollisionFilterGroups.DefaultFilter);
            _world.AddAction(_kinematicCharacterController);
        }

        public override void Dispose()
        {
            base.Dispose();

            _world.RemoveAction(_kinematicCharacterController);
            _world.RemoveCollisionObject(_ghostObject);

            _shape.Dispose();
            _ghostObject.Dispose();
        }

        internal override void Update(float dt)
        {
            var bulletWorldMatrix = _ghostObject.WorldTransform;
            var world = Conversion.ToTritonMatrix(ref bulletWorldMatrix);

            Position = Vector3.Transform(Vector3.Zero, world);

            if (TryJump && _kinematicCharacterController.CanJump)
            {
                _kinematicCharacterController.Jump();
            }

            _kinematicCharacterController.SetWalkDirection(Conversion.ToBulletVector(ref TargetVelocity) * dt);
        }

        public override void SetPosition(Vector3 position)
        {
            base.SetPosition(position);

            Position = position;

            var bulletPosition = Conversion.ToBulletVector(ref position);
            _kinematicCharacterController.Warp(ref bulletPosition);
        }
    }
}
