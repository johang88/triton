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
        [DataMember] public float Mass { get; set; } = 1.0f;

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
                Body = World.PhysicsWorld.CreateMeshBody(Mesh, Owner.Position, Mass, IsStatic ? Physics.BodyFlags.Static : Physics.BodyFlags.None);
            }
        }

        public override void Update(float dt)
        {
            if (Body == null && _mesh != null)
            {
                Body = World.PhysicsWorld.CreateMeshBody(Mesh, Owner.Position, Mass, IsStatic ? Physics.BodyFlags.Static : Physics.BodyFlags.None);
            }

            base.Update(dt);
        }
    }
}
