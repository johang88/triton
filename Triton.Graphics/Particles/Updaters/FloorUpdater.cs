using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Updaters
{
	public class FloorUpdater : IParticleUpdater
	{
		public float FloorPositionY = 0;
		public float BounceFactor = 0.5f;

		public void Update(float deltaTime, ParticleData particles)
		{
			for (var i = 0; i < particles.AliveCount; i++)
			{
				if (particles.Position[i].Y < FloorPositionY)
				{
					var force = particles.Acceleration[i];
					
					var normalFactor = Vector3.Dot(force, Vector3.UnitY);
					if (normalFactor < 0.0f)
						force -= Vector3.UnitY * normalFactor;

					float velocityFactor = Vector3.Dot(particles.Velocity[i], Vector3.UnitY);
					particles.Velocity[i] -= Vector3.UnitY * (1.0f + BounceFactor) * velocityFactor;

					particles.Acceleration[i] = force;
				}
			}
		}
	}
}
