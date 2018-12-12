using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class RenderShadowsParams
	{
		public int ModelViewProjection = 0;
		public int ClipPlane = 0;
		public int Bones = 0;
		public int Model = 0;
		public int ViewProjectionMatrices = 0;
		public int LightPosition = 0;
        public int LightDirectionAndBias = 0;
        public int View = 0;
        public int Projection = 0;
		public int ShadowBias = 0;
	}
}
