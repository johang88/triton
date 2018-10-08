using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	class AmbientLightParams
	{
		public int ModelViewProjection = 0;
		public int SamplerGBuffer0 = 0;
		public int SamplerGBuffer1 = 0;
		public int SamplerGBuffer2 = 0;
		public int SamplerDepth = 0;
        public int SamplerIrradiance = 0;
        public int SamplerSpecular = 0;
        public int SamplerSpecularIntegration = 0;
        public int IrradianceStrength = 0;
        public int SpecularStrength = 0;
		public int AmbientColor = 0;
        public int Mode = 0;
        public int CameraPosition = 0;
        public int InvViewProjection = 0;
    }
}
