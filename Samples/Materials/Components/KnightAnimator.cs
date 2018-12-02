using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Components;
using Triton.Graphics.SkeletalAnimation;

namespace Triton.Samples.Components
{
    public class KnightAnimator : GameObjectComponent
    {
        private AnimationState _idleState;

        public override void OnActivate()
        {
            base.OnActivate();

            _idleState = Owner.GetComponent<SkinnedMeshComponent>().GetAnimationState("walk");
            _idleState.Enabled = true;
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            _idleState.AddTime(dt);
        }
    }
}
