using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles
{
    public class ParticleEmitter
    {
        public float EmitRate = 100;
        public List<IParticleGenerator> Generators = new List<IParticleGenerator>();

        public void Emit(float deltaTime, ParticleData particles)
        {
            var maxNewParticles = (int)(deltaTime * EmitRate);
            var startId = particles.AliveCount;
            var endId = System.Math.Min(startId + maxNewParticles, particles.Count - 1);

            foreach (var generator in Generators)
            {
                generator.Generate(deltaTime, particles, startId, endId);
            }

            for (var i = startId; i < endId; i++)
            {
                particles.Wake(i);
            }
        }
    }
}
