using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Updaters
{
	public class EulerUpdater : IParticleUpdater
	{
		public Vector3 GlobalAcceleration = Vector3.Zero;

		public void Update(float deltaTime, ParticleData particles)
		{
			var acceleration = GlobalAcceleration * deltaTime;

			for (var i = 0; i < particles.AliveCount; i++)
			{
				particles.Acceleration[i] += acceleration;
			}

			for (var i = 0; i < particles.AliveCount; i++)
			{
				particles.Velocity[i] += particles.Acceleration[i] * deltaTime;
			}

			for (var i = 0; i < particles.AliveCount; i++)
			{
				particles.Position[i] += particles.Velocity[i] * deltaTime;
			}
		}
	}
}
