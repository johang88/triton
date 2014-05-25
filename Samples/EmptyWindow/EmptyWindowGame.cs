using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Samples
{
	class EmptyWindowGame : Triton.Samples.BaseGame
	{
		public EmptyWindowGame()
			: base("EmptyWindow")
		{
		}

		protected override void LoadResources()
		{
			base.LoadResources();

			Stage.ClearColor = new Triton.Vector4(0.5f, 0.5f, 0.7f, 0);
		}

		protected override void Update(float frameTime)
		{
			base.Update(frameTime);

			if (InputManager.IsKeyDown(Triton.Input.Key.Escape))
			{
				Running = false;
			}
		}
	}
}
