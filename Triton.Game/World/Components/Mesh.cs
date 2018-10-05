﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class Mesh : Component
	{
		public string Filename;
		public string Material;

		public Graphics.MeshInstance MeshInstance;
		public bool CastShadows = true;

		public override void OnActivate()
		{
			base.OnActivate();

			MeshInstance = World.Stage.AddMesh(Filename, Material);
		}

		public override void OnDetached()
		{
			base.OnDetached();

			World.Stage.RemoveMesh(MeshInstance);
		}

		public override void Update(float stepSize)
		{
			base.Update(stepSize);

			MeshInstance.World = Matrix4.Scale(Owner.Scale) * Owner.Orientation * Matrix4.CreateTranslation(Owner.Position);
			MeshInstance.CastShadows = CastShadows;
		}
	}
}
