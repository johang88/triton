using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Updaters
{
	public class BasicTimeUpdater : IParticleUpdater
	{
		public void Update(float deltaTime, ParticleData particles)
		{
			var endId = particles.AliveCount;
			for (var i = 0; i < endId; i++)
			{
				particles.Time[i].X -= deltaTime;
				particles.Time[i].Z = 1.0f - (particles.Time[i].X * particles.Time[i].W);

				if (particles.Time[i].X < 0.0f)
				{
					particles.Kill(i);
					endId = particles.AliveCount < particles.Count ? particles.AliveCount : particles.Count;
				}
			}
		}
	}
}
