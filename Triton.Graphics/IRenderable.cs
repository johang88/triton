using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public interface IRenderable
	{
		void AddRenderOperations(RenderOperations operations);
	}

	
}
