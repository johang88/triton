using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles
{
    public interface IParticleUpdater
    {
        void Update(float deltaTime, ParticleData particles);
    }
}
