using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles
{
    public class ParticleSystem
    {
        public ParticleData Particles { get; }

        [DataMember] public List<ParticleEmitter> Emitters { get; set; } = new List<ParticleEmitter>();
        [DataMember] public List<IParticleUpdater> Updaters { get; set; } = new List<IParticleUpdater>();

        [DataMember] public IParticleRenderer Renderer { get; set; }

        public ParticleSystem(int maxCount)
            => Particles = new ParticleData(maxCount);

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
            => Particles.AliveCount = 0;
    }
}
