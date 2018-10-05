using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class SkeletalMesh : Component
	{
		public string MeshFilename;

		private Graphics.MeshInstance MeshInstance;
		private Graphics.SkeletalAnimation.SkeletonInstance SkeletonInstance;

		public override void OnActivate()
		{
			base.OnActivate();

			MeshInstance = World.Stage.AddMesh(MeshFilename);

			SkeletonInstance = new Graphics.SkeletalAnimation.SkeletonInstance(MeshInstance.Mesh);
			MeshInstance.Skeleton = SkeletonInstance;

			SkeletonInstance.Play("run");
		}

		public override void OnDetached()
		{
			base.OnDetached();

			World.Stage.RemoveMesh(MeshInstance);
			World.ResourceManager.Unload(SkeletonInstance.Skeleton);
		}

		public override void Update(float dt)
		{
			base.Update(dt);

			SkeletonInstance.Update(dt);

			MeshInstance.World = Matrix4.Rotate(Owner.Orientation) * Matrix4.CreateTranslation(Owner.Position);
		}
	}
}
