using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World.Components
{
	public enum WaveFunction
	{
		Sin,
		Triangle,
		Square,
		Sawtooth,
		InvertedSawtooth,
		Noise
	}

	public class LightAnimator : Component
	{
		private static Random RNG = new Random();

		private PointLight Light;
		private float LightIntensity;

		public WaveFunction WaveFunction;
		public float Base = 0.0f;
		public float Amplitude = 1.0f;
		public float Phase = 0.0f;
		public float Frequency = 0.5f;

		private float ElapsedTime = 0.0f;

		public override void OnAttached(GameObject owner)
		{
			base.OnAttached(owner);

			Light = Owner.GetComponent<PointLight>();
			LightIntensity = Light.Intensity;
		}

		public override void Update(float stepSize)
		{
			base.Update(stepSize);

			ElapsedTime += stepSize;
			Light.Intensity = LightIntensity * EvaluateWave();
		}

		private float EvaluateWave()
		{
			var x = (ElapsedTime + Phase) * Frequency;
			x = x - (float)System.Math.Floor(x);

			float y;
			switch (WaveFunction)
			{
				case Components.WaveFunction.Sin:
					y = (float)System.Math.Sin(x * 2 * System.Math.PI);
					break;
				case Components.WaveFunction.Triangle:
					if (x < 0.5f)
						y = 4.0f * x - 1.0f;
					else
						y = -4.0f * x + 3.0f;
					break;
				case Components.WaveFunction.Square:
					if (x < 0.05f)
						y = 1.0f;
					else
						y = -1.0f;
					break;
				case Components.WaveFunction.Sawtooth:
					y = x;
					break;
				case Components.WaveFunction.InvertedSawtooth:
					y = 1.0f - x;
					break;
				case Components.WaveFunction.Noise:
					y = 1.0f - (float)RNG.NextDouble() * 2.0f;
					break;
				default:
					y = 1.0f;
					break;
			}

			y = y * 0.5f + 0.5f;

			return y * Amplitude + Base;
		}
	}
}
