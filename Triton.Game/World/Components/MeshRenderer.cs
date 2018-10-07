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
                }

                MeshInstance = null;
                _mesh = value;
            }
        }

        [DataMember] public bool CastShadows = true;

        private Graphics.MeshInstance MeshInstance;

        public override void OnActivate()
        {
            base.OnActivate();

            if (_mesh != null)
            {
                MeshInstance = World.Stage.AddMesh(_mesh);
            }
        }

        public override void OnDeactivate()
		{
			base.OnDeactivate();

            if (MeshInstance != null)
            {
                World.Stage.RemoveMesh(MeshInstance);
            }
        }

		public override void Update(float dt)
		{
			base.Update(dt);

            if (MeshInstance == null && _mesh != null)
            {
                MeshInstance = World.Stage.AddMesh(_mesh);
            }
            else if (MeshInstance != null)
            {
                MeshInstance.World = Matrix4.Scale(Owner.Scale) * Matrix4.Rotate(Owner.Orientation) * Matrix4.CreateTranslation(Owner.Position);
                MeshInstance.CastShadows = CastShadows;
            }
		}
	}
}
