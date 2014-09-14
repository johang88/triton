using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class LightParams
	{
		public int ModelViewProjection = 0;
		public int SamplerGBuffer0 = 0;
		public int SamplerGBuffer1 = 0;
		public int SamplerGBuffer2 = 0;
		public int samplerDepth = 0;
		public int SamplerShadow = 0;
		public int SamplerShadowCube = 0;
		public int LightDirection = 0;
		public int CameraPosition = 0;
		public int LightPosition = 0;
		public int LightColor = 0;
		public int LightRange = 0;
		public int ScreenSize = 0;
		public int SpotParams = 0;
		public int InvViewProjection = 0;
		public int ShadowViewProj = 0;
		public int ShadowBias = 0;
		public int ClipPlane = 0;
		public int TexelSize = 0;
		public int LightInvSquareRadius = 0;
	}
}
