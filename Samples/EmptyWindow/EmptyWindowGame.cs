using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Samples
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

			// Mount the sample data
			FileSystem.AddPackage("FileSystem", "../Data/samples_data");
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
