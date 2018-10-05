using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World
{
	public interface IComponent
	{
        /// <summary>
        /// Called when activated on the world
        /// </summary>
        void OnActivate();
        /// <summary>
        /// Called when decativated from the world
        /// </summary>
        void OnDeactivate();

        /// <summary>
        /// Called when attached to a game object
        /// </summary>
        /// <param name="owner"></param>
		void OnAttached(GameObject owner);
        /// <summary>
        /// Called when detached from a game object
        /// </summary>
		void OnDetached();

		void Update(float dt);

		GameObject Owner { get; }
	}
}
