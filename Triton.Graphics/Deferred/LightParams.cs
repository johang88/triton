using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class LightParams
	{
		public int HandleModelViewProjection = 0;
		public int HandleNormalTexture = 0;
		public int HandlePositionTexture = 0;
		public int HandleSpecularTexture = 0;
		public int HandleDiffuseTexture = 0;
		public int HandleShadowMap = 0;
		public int HandleLightDirection = 0;
		public int HandleCameraPosition = 0;
		public int HandleLightPosition = 0;
		public int HandleLightColor = 0;
		public int HandleLightRange = 0;
		public int HandleScreenSize = 0;
		public int HandleSpotLightParams = 0;
		public int HandleInverseViewMatrix = 0;
		public int ShadowViewProjection = 0;
		public int HandleShadowBias = 0;
		public int HandleClipPlane = 0;
	}
}
