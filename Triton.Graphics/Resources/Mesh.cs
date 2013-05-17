using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Mesh : Triton.Common.Resource
	{
		public int[] Handles { get; internal set; }

		public Mesh(string name, string parameters)
			: base(name, parameters)
		{
			Handles = new int[0];
		}
	}
}
