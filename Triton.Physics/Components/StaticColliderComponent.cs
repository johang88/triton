using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Components
{
    public class StaticColliderComponent : BasePhysicsComponent
    {
        public override void OnActivate()
        {
            _nativeCollisionObject = new BulletSharp.CollisionObject
            {
                CollisionShape = NativeCollisionShape,
                UserObject = this
            };

            _nativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.NoContactResponse;

            PhysicsWorld.DiscreteDynamicsWorld.AddCollisionObject(_nativeCollisionObject, BulletSharp.CollisionFilterGroups.AllFilter, BulletSharp.CollisionFilterGroups.AllFilter);

            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            if (_nativeCollisionObject != null)
            {
                PhysicsWorld.DiscreteDynamicsWorld.RemoveCollisionObject(_nativeCollisionObject);
            }

            base.OnDeactivate();
        }
    }
}
