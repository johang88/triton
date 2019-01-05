using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Components
{
    public abstract class RenderableComponent : GameObjectComponent
    {
        [DataMember] public bool CastShadows { get; set; } = true;

        public BoundingSphere BoundingSphere;
        public BoundingBox BoundingBox;

        internal Stage Stage => Owner.World.Services.Get<Stage>();

        public abstract void PrepareRenderOperations(BoundingFrustum frustum, RenderOperations operations);

        public override void OnActivate()
        {
            base.OnActivate();

            Stage.AddRenderableComponent(this);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            Stage.RemoveRenderableComponent(this);
        }
    }
}
