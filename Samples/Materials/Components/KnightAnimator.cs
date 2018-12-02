using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Components;

namespace Triton.Samples.Components
{
    public class KnightAnimator : GameObjectComponent
    {
        public override void OnActivate()
        {
            base.OnActivate();

            Owner.GetComponent<SkinnedMeshComponent>().Play("idle");
        }
    }
}
