using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class DirectionalLightParams
	{
		public int HandleMVP;
		public int HandleNormal;
		public int HandlePosition;
		public int HandleSpecular;
		public int HandleDiffuse;

		public int HandleCameraPosition;

		public int HandleLightDirection;
		public int HandleLightColor;
		public int HandleScreenSize;
	}
}
