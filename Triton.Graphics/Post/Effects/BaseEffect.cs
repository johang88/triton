using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Post.Effects
{
	public class BaseEffect
	{
		protected readonly Backend Backend;
		protected readonly BatchBuffer QuadMesh;
		protected readonly Common.ResourceManager ResourceManager;

		public BaseEffect(Backend backend, Common.ResourceManager resourceManager, BatchBuffer quadMesh)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (quadMesh == null)
				throw new ArgumentNullException("quadMesh");

			Backend = backend;
			ResourceManager = resourceManager;
			QuadMesh = quadMesh;
		}
	}
}
