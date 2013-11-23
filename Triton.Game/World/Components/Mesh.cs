using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class Mesh : Component
	{
		public string Filename;

		private Graphics.MeshInstance MeshInstance;

		public override void OnActivate()
		{
			base.OnActivate();

			MeshInstance = World.Stage.AddMesh(Filename);
		}

		public override void OnDeactivate()
		{
			base.OnDeactivate();

			World.Stage.RemoveMesh(MeshInstance);
		}

		public override void Update(float stepSize)
		{
			base.Update(stepSize);

			
		}
	}
}
