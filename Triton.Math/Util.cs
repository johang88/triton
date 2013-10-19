using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Math
{
	public class Util
	{
		public static float Lerp(float x, float y, float a)
		{
			return x + a * (y - x);
		}
	}
}
