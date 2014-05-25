using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.HDR
{
	class AdaptLuminanceParams
	{
		public int ModelViewProjection = 0;
		public int SamplerLastLuminacne = 0;
		public int SamplerCurrentLuminance = 0;
		public int TimeDelta = 0;
		public int Tau = 0;
	}
}
