using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Updaters
{
	public class BasicColorUpdater : IParticleUpdater
	{
		public void Update(float deltaTime, ParticleData particles)
		{
			for (var i = 0; i < particles.AliveCount; i++)
			{
				Vector4.Lerp(ref particles.StartColor[i], ref particles.EndColor[i], particles.Time[i].Z, out particles.Color[i]);
			}
		}
	}
}
