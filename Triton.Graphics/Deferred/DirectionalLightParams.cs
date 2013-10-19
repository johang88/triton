using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class DirectionalLightParams
	{
		public int HandleModelViewProjection = 0;
		public int HandleNormalTexture = 0;
		public int HandlePositionTexture = 0;
		public int HandleSpecularTexture = 0;
		public int HandleDiffuseTexture = 0;
		public int HandleCameraPosition = 0;
		public int HandleLightDirection = 0;
		public int HandleLightColor = 0;
		public int HandleScreenSize = 0;
	}
}
