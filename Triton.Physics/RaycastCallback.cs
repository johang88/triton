using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics
{
	public delegate bool RaycastCallback(Body body, Vector3 normal, float fraction);
}
