using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
	public sealed class GameWorld
	{
		public readonly Triton.Physics.World PhysicsWorld;
		public readonly Triton.Graphics.Stage GraphicsStage;
		public readonly Triton.Graphics.Camera Camera = new Graphics.Camera(Vector2.Zero);
		
		public GameWorld(Common.ResourceManager resourceManager, Graphics.Backend backend)
		{
			PhysicsWorld = new Physics.World(backend, resourceManager);
			GraphicsStage = new Graphics.Stage(resourceManager);
		}

		public void Clear()
		{
			GraphicsStage.Clear();
			//PhysicsWorld.Clear();
		}
	}
}
