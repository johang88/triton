using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World
{
	public interface IComponent
	{
		void OnAttached(GameObject owner);
		void OnDeactivate();

		void OnActivate();

		void Update(float stepSize);

		GameObject Owner { get; }
	}
}
