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
            => ComponentType = componentType ?? throw new ArgumentNullException("componentType");

        public readonly Type ComponentType;
	}
}
