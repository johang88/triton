using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Generators
{
	public class BasicColorGenerator : IParticleGenerator
	{
		public Vector4 MinStartColor = Vector4.Zero;
		public Vector4 MaxStartColor = Vector4.Zero;

		public Vector4 MinEndColor = Vector4.Zero;
		public Vector4 MaxEndColor = Vector4.Zero;

		public void Generate(float deltaTime, ParticleData particles, int startId, int endId)
		{
			for (var i = startId; i < endId; i++)
			{
				var angle = Math.Util.Random(0.0f, Math.Util.TwoPi);

				Math.Util.Random(ref MinStartColor, ref MaxStartColor, out particles.StartColor[i]);
				Math.Util.Random(ref MinEndColor, ref MaxEndColor, out particles.EndColor[i]);
			}
		}
	}
}
