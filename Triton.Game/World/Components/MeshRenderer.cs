using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Resources;

namespace Triton.Game.World.Components
{
	public class MeshRenderer : Component
	{
        private Mesh _mesh = null;
        [DataMember] public Mesh Mesh
        {
            get => _mesh;
            set
            {
                if (MeshInstance != null)
                {
                    World.Stage.RemoveMesh(MeshInstance);
                    World.ResourceManager.Unload(_mesh);
                }

                MeshInstance = null;
                _mesh = value;

                if (Owner != null && World != null)
                {
                    // Assume "ownership"
                    World.ResourceManager.AddReference(_mesh);
                    MeshInstance = World.Stage.AddMesh(_mesh);
                }
            }
        }

		public Graphics.MeshInstance MeshInstance { get; private set; }
        [DataMember] public bool CastShadows = true;

		public override void OnActivate()
		{
			base.OnActivate();

            // Assume "ownership"
            World.ResourceManager.AddReference(_mesh);
            MeshInstance = World.Stage.AddMesh(Mesh);
        }

		public override void OnDeactivate()
		{
			base.OnDeactivate();

            if (MeshInstance != null)
            {
                World.Stage.RemoveMesh(MeshInstance);
                World.ResourceManager.Unload(_mesh);
            }
        }

		public override void Update(float dt)
		{
			base.Update(dt);

			MeshInstance.World = Matrix4.Scale(Owner.Scale) * Matrix4.Rotate(Owner.Orientation) * Matrix4.CreateTranslation(Owner.Position);
			MeshInstance.CastShadows = CastShadows;
		}
	}
}
