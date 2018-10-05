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
        protected GameObjectManager World => Owner.World;
        protected Graphics.Stage Stage => Owner.World.Stage;
        protected GameObject Parent => Owner.Parent;

		public virtual void OnAttached(GameObject owner)
		{
            Owner = owner ?? throw new ArgumentNullException("owner");
		}

		public virtual void OnActivate()
		{

		}

        public virtual void OnDeactivate()
        {

        }

		public virtual void OnDetached()
		{

		}

		public virtual void Update(float dt)
		{

		}
	}
}
