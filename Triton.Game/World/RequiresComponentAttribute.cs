using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World
{
	public class RequiresComponentAttribute : Attribute
	{
		public RequiresComponentAttribute(Type componentType)
		{
			if (componentType == null)
				throw new ArgumentNullException("componentType");

			ComponentType = componentType;
		}

		public readonly Type ComponentType;
	}
}
