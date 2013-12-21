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
		public string SkeletonFilename;

		private Graphics.MeshInstance MeshInstance;
		private Graphics.SkeletalAnimation.SkeletonInstance SkeletonInstance;

		public override void OnAttached(GameObject owner)
		{
			base.OnAttached(owner);

			MeshInstance = World.Stage.AddMesh(MeshFilename);
			
			var skeleton = World.ResourceManager.Load<Graphics.SkeletalAnimation.Skeleton>(SkeletonFilename);
			SkeletonInstance = new Graphics.SkeletalAnimation.SkeletonInstance(skeleton);

			MeshInstance.Skeleton = SkeletonInstance;
		}

		public override void OnDetached()
		{
			base.OnDetached();

			World.Stage.RemoveMesh(MeshInstance);
			World.ResourceManager.Unload(SkeletonInstance.Skeleton);
		}

		public override void Update(float stepSize)
		{
			base.Update(stepSize);

			SkeletonInstance.Update(stepSize);

			MeshInstance.World = Owner.Orientation * Matrix4.CreateTranslation(Owner.Position);
		}
	}
}
