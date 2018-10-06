using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class PointLight : Component
	{
		private Graphics.Light Light;

        [DataMember] public float Range = 16;
        [DataMember] public Vector3 Color = new Vector3(1, 1, 1);
        [DataMember] public float ShadowBias = 0.005f;
        [DataMember] public bool CastShadows = true;
        [DataMember] public float Intensity = 10;

		public override void OnActivate()
		{
			base.OnActivate();

			Light = Stage.CreatePointLight(Owner.Position, Range, Color, CastShadows, ShadowBias, Intensity);
		}

		public override void OnDeactivate()
		{
			base.OnDeactivate();

			Stage.RemoveLight(Light);
		}

		public override void Update(float dt)
		{
			base.Update(dt);

			Light.Position = Owner.Position;
			Light.Range = Range;
			Light.Color = Color;
			Light.ShadowBias = ShadowBias;
			Light.CastShadows = CastShadows;
			Light.Intensity = Intensity;
		}
	}
}
