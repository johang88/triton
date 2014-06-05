using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Math
{
	public class Intersections
	{
		public static bool SphereToSphere(ref Vector3 p1, float r1, ref Vector3 p2, float r2)
		{
			Vector3 distance;
			Vector3.Subtract(ref p2, ref p1, out distance);

			return distance.LengthSquared <= (r1 + r2) * (r1 + r2);
		}
	}
}
