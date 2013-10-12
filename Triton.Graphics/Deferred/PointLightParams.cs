using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class PointLightParams
	{
		public int HandleMVP;
		public int HandleNormal;
		public int HandlePosition;
		public int HandleSpecular;

		public int HandleCameraPosition;

		public int HandleLightPositon;
		public int HandleLightColor;
		public int HandleLightRange;
		public int HandleScreenSize;
	}
}
