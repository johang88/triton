using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Resources;

namespace Triton.Graphics
{
    public class AmbientLight
    {
        public Texture Irradiance { get; set; }
        public float IrradianceStrength { get; set; } = 1.0f;
        public Texture Specular { get; set; }
        public float SpecularStrength { get; set; } = 1.0f;
    }
}
