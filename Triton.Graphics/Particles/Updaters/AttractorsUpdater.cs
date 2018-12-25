	using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Updaters
{
	public class AttractorsUpdater : IParticleUpdater
	{
		public readonly List<Vector4> Attractors = new List<Vector4>();

		public void Update(float deltaTime, ParticleData particles)
		{
			Vector3 offset = new Vector3();

			for (var i = 0; i < particles.AliveCount; i++)
			{
				for (var a = 0;  a < Attractors.Count; a++)
				{
					var attractor = Attractors[a];

					offset.X = attractor.X - particles.Position[i].X;
					offset.Y = attractor.Y - particles.Position[i].Y;
					offset.Z = attractor.Z - particles.Position[i].Z;

					float dist;
					Vector3.Dot(ref offset, ref offset, out dist);

					dist = attractor.W / dist;

					particles.Acceleration[i] += offset * dist;
				}
			}
		}
	}
}
