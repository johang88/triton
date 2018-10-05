using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class MeshRigidBody : RigidBody
	{
		private Physics.Resources.Mesh Mesh;
		public string Filename;
		public bool IsStatic = true;

		public override void OnActivate()
		{
			base.OnActivate();

            Mesh = World.ResourceManager.Load<Physics.Resources.Mesh>(Filename);
            Body = World.PhysicsWorld.CreateMeshBody(Mesh, Owner.Position, IsStatic);
		}
	}
}
