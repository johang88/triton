using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Generators
{
	public class CirclePositionGenerator : IParticleGenerator
	{
		public Vector3 Position = Vector3.Zero;
		public float RadiusX = 1;
		public float RadiusZ = 1;

		public void Generate(float deltaTime, ParticleData particles, int startId, int endId)
		{
			for (var i = startId; i < endId; i++)
			{
				var angle = Math.Util.Random(0.0f, Math.Util.TwoPi);

				particles.Position[i] = Position;

				particles.Position[i].X += RadiusX * (float)System.Math.Sin(angle);
				particles.Position[i].Z += RadiusZ * (float)System.Math.Cos(angle);
			}
		}
	}
}