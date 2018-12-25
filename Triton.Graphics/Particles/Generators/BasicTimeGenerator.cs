using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Generators
{
	public class BasicTimeGenerator : IParticleGenerator
	{
		public float MinTime = 0.0f;
		public float MaxTime = 0.0f;

		public void Generate(float deltaTime, ParticleData particles, int startId, int endId)
		{
			for (var i = startId; i < endId; i++)
			{
				particles.Time[i].X = particles.Time[i].Y = Math.Util.Random(MinTime, MaxTime);
				particles.Time[i].Z = 0.0f;
				particles.Time[i].W = 1.0f / particles.Time[i].X;
			}
		}
	}
}
