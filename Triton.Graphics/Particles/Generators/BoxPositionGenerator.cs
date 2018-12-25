using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Generators
{
	public class BoxPositionGenerator : IParticleGenerator
	{
		public Vector3 Position = Vector3.Zero;
		public Vector3 MaxStartPosOffset = Vector3.Zero;

		public void Generate(float deltaTime, ParticleData particles, int startId, int endId)
		{
			var minPosition = Position - MaxStartPosOffset;
			var maxPosition = Position + MaxStartPosOffset;

			for (var i = startId; i < endId; i++)
			{
				particles.Position[i].X = Math.Util.Random(minPosition.X, maxPosition.X);
				particles.Position[i].Y = Math.Util.Random(minPosition.Y, maxPosition.Y);
				particles.Position[i].Z = Math.Util.Random(minPosition.Z, maxPosition.Z);
			}
		}
	}
}
