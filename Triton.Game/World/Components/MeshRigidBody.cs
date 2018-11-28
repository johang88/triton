using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Physics;

namespace Triton.Game.World.Components
{
	public class MeshRigidBody : RigidBody
	{
        private Physics.Resources.Mesh _mesh = null;
        [DataMember] public Physics.Resources.Mesh Mesh
        {
            get => _mesh;
            set
            {
                if (_body != null)
                {
                    World.PhysicsWorld.RemoveBody(_body);
                }

                _body = null;
                _mesh = value;
            }
        }

        protected override Body CreateBody(BodyFlags flags)
            => World.PhysicsWorld.CreateMeshBody(Mesh, Owner.Position, Mass, IsStatic ? Physics.BodyFlags.Static : Physics.BodyFlags.None);

        public override void Update(float dt)
        {
            if (_body == null && _mesh != null)
            {
                _body = World.PhysicsWorld.CreateMeshBody(Mesh, Owner.Position, Mass, IsStatic ? Physics.BodyFlags.Static : Physics.BodyFlags.None);
            }

            base.Update(dt);
        }
    }
}
