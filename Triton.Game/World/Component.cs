using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World
{
	public class Component : IComponent
	{
		public GameObject Owner { get; private set; }
		protected GameObjectManager World { get; private set; }
		protected Graphics.Stage Stage { get; private set; }
		protected GameObject Parent { get { return Owner.Parent; } }

		public virtual void OnAttached(GameObject owner)
		{
            Owner = owner ?? throw new ArgumentNullException("owner");
			World = Owner.World;
			Stage = World.Stage;
		}

		public virtual void OnActivate()
		{

		}

		public virtual void OnDetached()
		{

		}

		public virtual void Update(float stepSize)
		{

		}
	}
}
