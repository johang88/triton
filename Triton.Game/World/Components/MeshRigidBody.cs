using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class MeshRigidBody : RigidBody
	{
        [DataMember] public bool IsStatic { get; set; } = true;

        private Physics.Resources.Mesh _mesh = null;
        [DataMember] public Physics.Resources.Mesh Mesh
        {
            get => _mesh;
            set
            {
                if (Body != null)
                {
                    World.PhysicsWorld.RemoveBody(Body);
                }

                Body = null;
                _mesh = value;
            }
        }

        public override void OnActivate()
        {
            base.OnActivate();

            if (_mesh != null)
            {
                Body = World.PhysicsWorld.CreateMeshBody(Mesh, Owner.Position, IsStatic);
            }
        }

        public override void Update(float dt)
        {
            if (Body == null && _mesh != null)
            {
                Body = World.PhysicsWorld.CreateMeshBody(Mesh, Owner.Position, IsStatic);
            }

            base.Update(dt);
        }
    }
}
