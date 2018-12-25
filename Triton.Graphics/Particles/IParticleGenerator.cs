using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles
{
    public interface IParticleGenerator
    {
        void Generate(float deltaTime, ParticleData particles, int startId, int endId);
    }
}
