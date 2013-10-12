using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public enum LighType
	{
		PointLight,
		SpotLight,
		Directional
	}

	public class Light
	{
		public LighType Type;

		public Vector3 Position;
		public Vector3 Direction;

		public Vector3 Color;

		public float Range;

		public float InnerAngle;
		public float OuterAngle;

		public bool Enabled = true;
	}
}
