using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Post
{
	public struct HDRSettings
	{
		public float KeyValue;
		public float AdaptationRate;
		public float BlurSigma;
		public float BloomThreshold;

		public bool EnableBloom;
		public bool EnableLensFlares;
	}
}
