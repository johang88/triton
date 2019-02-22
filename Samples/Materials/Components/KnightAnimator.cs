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
        //private SkinnedMeshComponent _skinnedMeshComponent;
        //private AnimationState _animationState;
        //private int _currentAniamtion = 0;

        public override void OnActivate()
        {
            base.OnActivate();

            //_skinnedMeshComponent = Owner.GetComponent<SkinnedMeshComponent>();
            //NextAnimation();
        }

        //public void SetActiveAnimation(string name)
        //{
        //    if (_animationState?.Name == name)
        //        return;

        //    if (_animationState != null)
        //    {
        //        _animationState.Enabled = false;
        //    }

        //    _animationState = _skinnedMeshComponent.GetAnimationState(name);

        //    if (_animationState != null)
        //    {
        //        _animationState.Enabled = true;
        //        _animationState.TimePosition = 0;
        //    }
        //}

        //public void NextAnimation()
        //{
        //    _currentAniamtion = ++_currentAniamtion % _skinnedMeshComponent.Skeleton.Animations.Length;
        //    SetActiveAnimation(_skinnedMeshComponent.Skeleton.Animations[_currentAniamtion].Name);
        //}

        public override void Update(float dt)
        {
            base.Update(dt);

            // TODO: Proper child transforms
            Owner.Position = Owner.Parent.Position - new Vector3(0, 0.9f, 0);
            Owner.Orientation = Owner.Parent.Orientation * Quaternion.FromAxisAngle(Vector3.UnitY, Math.Util.DegreesToRadians(180.0f));

            //if (_animationState != null)
            //{
            //    _animationState.AddTime(dt);
            //}
        }
    }
}
