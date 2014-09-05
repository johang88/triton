using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.CustomNodes
{
	static class Context
	{
		private static int Counter = 0;

		public static string NextVariable(string prefix)
		{
			return prefix + "_" + Common.StringConverter.ToString(Counter++);
		}
	}
}
