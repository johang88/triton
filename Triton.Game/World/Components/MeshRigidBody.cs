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
                    World.ResourceManager.Unload(Mesh);
                }

                Body = null;
                _mesh = value;

                if (Owner != null && World != null)
                {
                    World.ResourceManager.AddReference(Mesh);
                    Body = World.PhysicsWorld.CreateMeshBody(Mesh, Owner.Position, IsStatic);
                }
            }
        }

		public override void OnActivate()
		{
			base.OnActivate();
            Body = World.PhysicsWorld.CreateMeshBody(Mesh, Owner.Position, IsStatic);
            World.ResourceManager.AddReference(Mesh);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            World.ResourceManager.Unload(Mesh);
        }
    }
}
