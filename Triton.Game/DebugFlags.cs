using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game
{
	[Flags]
	public enum DebugFlags
	{
		GBuffer = 1,
		Physics = 2,
		RenderStats = 4
	}
}
