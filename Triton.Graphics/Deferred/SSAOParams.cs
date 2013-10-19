using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class SSAOParams
	{
		public int HandleMVP;
		public int HandlePosition;
		public int HandleNormal;
		public int HandleRandom;

		public int HandleNoiseScale;
		public int HandleSampleKernel;
		public int HandleViewMatrix;
		public int HandleProjectionMatrix;
	}
}
