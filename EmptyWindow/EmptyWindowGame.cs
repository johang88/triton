using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmptyWindow
{
	class EmptyWindowGame : Triton.Game.Game
	{
		public EmptyWindowGame()
			: base("EmptyWindow")
		{
			Width = 1280;
			Height = 720;
		}

		protected override void MountFileSystem()
		{
			base.MountFileSystem();

			// Mount the core resource package, this is required
			FileSystem.AddPackage("FileSystem", "../Data/core_data");
		}

		protected override void LoadResources()
		{
			base.LoadResources();

			Stage.ClearColor = new Triton.Vector4(1, 0, 0, 0);
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
