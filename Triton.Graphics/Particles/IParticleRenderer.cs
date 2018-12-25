using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles
{
    public interface IParticleRenderer
    {
        void Update(ParticleSystem particleSystem, Stage stage, float deltaTime);
        void PrepareRenderOperations(ParticleSystem particleSystem, RenderOperations operations, Matrix4 worldOffset);
    }
}
