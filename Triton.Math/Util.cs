using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Math
{
	public class Util
	{
		public const float Pi = 3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117067982148086513282306647093844609550582231725359408128481117450284102701938521105559644622948954930382f;
		public const float PiOver2 = Pi / 2;
		public const float PiOver3 = Pi / 3;
		public const float PiOver4 = Pi / 4;
		public const float PiOver6 = Pi / 6;
		public const float TwoPi = 2 * Pi;
		public const float ThreePiOver2 = 3 * Pi / 2;
		public const float E = 2.71828182845904523536f;
		public const float Log10E = 0.434294482f;
		public const float Log2E = 1.442695041f;

		public static float DegreesToRadians(float degrees)
		{
			const float degToRad = (float)System.Math.PI / 180.0f;
			return degrees * degToRad;
		}

		public static float RadiansToDegrees(float radians)
		{
			const float radToDeg = 180.0f / (float)System.Math.PI;
			return radians * radToDeg;
		}

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
