using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Deferred
{
	public class FogSettings
	{
		public bool Enable = true;
		public float Start = 0;
		public float End = 2000.0f;
		public Vector3 Color = new Vector3(1, 1, 1);
	}
}
