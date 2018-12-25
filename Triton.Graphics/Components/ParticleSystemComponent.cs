using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Components
{
    public class ParticleSystemComponent : RenderableComponent
    {
        [DataMember] public Particles.ParticleSystem ParticleSystem { get; set; }

        public override void PrepareRenderOperations(RenderOperations operations)
        {
            if (ParticleSystem == null || ParticleSystem.Renderer == null)
                return;

            Owner.GetWorldMatrix(out var world);
            ParticleSystem.Renderer.PrepareRenderOperations(ParticleSystem, operations, world);
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            BoundingSphereRadius = 100f;
            ParticleSystem?.Update(dt);
        }
    }
}
