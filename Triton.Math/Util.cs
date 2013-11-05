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

		public static float Square(float x)
		{
			return x * x;
		}

		public static float GaussianDistribution(float x, float offset, float scale)
		{
			var nom = (float)System.Math.Exp(-Square(x - offset) / (2 * Square(scale)));
			var denom = scale * (float)System.Math.Sqrt(2 * System.Math.PI);

			return nom / denom;
		}
	}
}
