using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class GBufferParams
	{
		public int HandleMVP;
		public int HandleWorld;
		public int HandleDiffuseTexture;
		public int HandleNormalMap;
		public int HandleSpecularMap;
	}
}
