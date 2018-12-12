using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Components
{
    public class LightComponent : GameObjectComponent
    {
        [DataMember] public LighType Type { get; set; }
        [DataMember] public Vector3 Color { get; set; }
        [DataMember] public float Intensity { get; set; } = 1.0f;
        [DataMember] public float Range { get; set; }
        [DataMember] public float InnerAngle { get; set; }
        [DataMember] public float OuterAngle { get; set; }
        [DataMember] public bool CastShadows { get; set; } = false;
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public float ShadowBias { get; set; } = 0.01f;
        [DataMember] public float ShadowNearClipDistance { get; set; } = 0.01f;

        internal Stage Stage => Owner.World.Services.Get<Stage>();

        public override void OnActivate()
        {
            base.OnActivate();

            Stage.AddLightComponent(this);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            Stage.RemoveLightComponent(this);
        }
    }
}
