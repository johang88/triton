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

        public void Emit(float deltaTime, ParticleData particles, Vector3 position, Quaternion orientation)
        {
            var maxNewParticles = (int)(deltaTime * EmitRate);
            var startId = particles.AliveCount;
            var endId = System.Math.Min(startId + maxNewParticles, particles.Count - 1);

            foreach (var generator in Generators)
            {
                generator.Generate(deltaTime, particles, startId, endId);
            }

            Matrix4.Rotate(ref orientation, out var rotation);
            Matrix4.CreateTranslation(ref position, out var translation);
            Matrix4.Mult(ref rotation, ref translation, out var world);

            for (var i = startId; i < endId; i++)
            {
                Vector3.Transform(ref particles.Position[i], ref world, out particles.Position[i]);
            }

            for (var i = startId; i < endId; i++)
            {
                particles.Wake(i);
            }
        }
    }
}
