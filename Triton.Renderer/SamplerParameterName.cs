using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer
{
	public enum SamplerParameterName
	{
		TextureBorderColor = 4100,
		TextureMagFilter = 10240,
		TextureMinFilter = 10241,
		TextureWrapS = 10242,
		TextureWrapT = 10243,
		TextureWrapR = 32882,
		TextureMinLod = 33082,
		TextureMaxLod = 33083,
		TextureMaxAnisotropyExt = 34046,
		TextureLodBias = 34049,
		TextureCompareMode = 34892,
		TextureCompareFunc = 34893,
	}
}
