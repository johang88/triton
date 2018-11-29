using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Utility;

namespace Triton.Content.Materials.CustomNodes
{
	static class Context
	{
		private static int Counter = 0;
		public static readonly Dictionary<string, string> Samplers = new Dictionary<string, string>();

		public static void Reset()
		{
			Counter = 0;
			Samplers.Clear();
		}

		public static string NextVariable(string prefix)
		{
			return prefix + "_" + StringConverter.ToString(Counter++);
		}
	}
}
