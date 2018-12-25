using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles
{
    public class ParticleSystem
    {
        public ParticleData Particles;

        public List<ParticleEmitter> Emitters = new List<ParticleEmitter>();
        public List<IParticleUpdater> Updaters = new List<IParticleUpdater>();

        public ParticleSystem(int maxCount)
        {
            Particles = new ParticleData(maxCount);
        }

        public void Update(float deltaTime)
        {
            foreach (var emitter in Emitters)
            {
                emitter.Emit(deltaTime, Particles);
            }

            foreach (var updater in Updaters)
            {
                updater.Update(deltaTime, Particles);
            }
        }

        public void Reset()
        {
            Particles.AliveCount = 0;
        }
    }
}
