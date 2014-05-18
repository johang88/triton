﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer
{
	public enum TextureMinFilter
	{
		Nearest = 9728,
		Linear = 9729,
		NearestMipmapNearest = 9984,
		LinearMipmapNearest = 9985,
		NearestMipmapLinear = 9986,
		LinearMipmapLinear = 9987,
		Filter4Sgis = 33094,
		LinearClipmapLinearSgix = 33136,
		PixelTexGenQCeilingSgix = 33156,
		PixelTexGenQRoundSgix = 33157,
		PixelTexGenQFloorSgix = 33158,
		NearestClipmapNearestSgix = 33869,
		NearestClipmapLinearSgix = 33870,
		LinearClipmapNearestSgix = 33871,
	}
}
