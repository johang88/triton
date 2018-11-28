using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Post.Effects
{
	public class BaseEffect
	{
		protected readonly Backend _backend;
		protected readonly BatchBuffer _quadMesh;

		public BaseEffect(Backend backend, BatchBuffer quadMesh)
		{
            _backend = backend ?? throw new ArgumentNullException("backend");
			_quadMesh = quadMesh ?? throw new ArgumentNullException("quadMesh");
		}

		internal virtual void LoadResources(Triton.Resources.ResourceManager resourceManager)
		{
		}

		public virtual void Resize(int width, int height)
		{

		}
	}
}
