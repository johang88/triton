using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Texture : Triton.Common.Resource
	{
		public int Handle { get; internal set; }

		public Texture(string name, string parameters)
			: base(name, parameters)
		{
			Handle = -1;
		}
	}
}
