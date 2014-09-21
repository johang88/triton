using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public class PointLight : Component
	{
		private Graphics.Light Light;

		public float Range = 16;
		public Vector3 Color = new Vector3(1, 1, 1);
		public float ShadowBias = 0.005f;
		public bool CastShadows = true;
		public float Intensity = 10;

		public override void OnActivate()
		{
			base.OnActivate();

			Light = Stage.CreatePointLight(Owner.Position, Range, Color, CastShadows, ShadowBias, Intensity);
		}

		public override void OnDetached()
		{
			base.OnDetached();

			Stage.RemoveLight(Light);
		}

		public override void Update(float stepSize)
		{
			base.Update(stepSize);

			Light.Position = Owner.Position;
			Light.Range = Range;
			Light.Color = Color;
			Light.ShadowBias = ShadowBias;
			Light.CastShadows = CastShadows;
			Light.Intensity = Intensity;
		}
	}
}
