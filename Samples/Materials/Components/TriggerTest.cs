using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Game.World;
using Triton.Game.World.Components;

namespace Triton.Samples.Components
{
    class TriggerTest : BaseComponent
    {
        private PointLight _light;

        public override void OnActivate()
        {
            base.OnActivate();

            //_body = Owner.GetComponent<RigidBody>();
            //_body.Collision += OnCollision;

            _light = Owner.GetComponent<PointLight>();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            //_body.Collision -= OnCollision;
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            _light.Enabled = false;
        }

        private void OnCollision(GameObject other)
        {
            //if (other.HasComponent<CharacterController>())
            //{
            //    _light.Enabled = true;
            //}
        }
    }
}
