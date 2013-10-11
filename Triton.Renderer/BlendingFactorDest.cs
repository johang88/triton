using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer
{
	public enum BlendingFactorDest
	{
		Zero = 0,
		One = 1,
		SrcColor = 768,
		OneMinusSrcColor = 769,
		SrcAlpha = 770,
		OneMinusSrcAlpha = 771,
		DstAlpha = 772,
		OneMinusDstAlpha = 773,
		ConstantColor = 32769,
		ConstantColorExt = 32769,
		OneMinusConstantColorExt = 32770,
		OneMinusConstantColor = 32770,
		ConstantAlpha = 32771,
		ConstantAlphaExt = 32771,
		OneMinusConstantAlphaExt = 32772,
		OneMinusConstantAlpha = 32772,
		Src1Alpha = 34185,
		Src1Color = 35065,
		OneMinusSrc1Color = 35066,
		OneMinusSrc1Alpha = 35067,
	}
}
