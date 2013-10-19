using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class GBufferParams
	{
		public int ModelViewProjection = 0;
		public int HandleWorld = 0;
		public int HandleWorldView = 0;
		public int HandleITWorldView = 0;
		public int HandleDiffuseTexture = 0;
		public int HandleNormalMap = 0;
		public int HandleSpecularMap = 0;
	}
}
