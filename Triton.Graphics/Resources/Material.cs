using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Material : Triton.Common.Resource
	{
		public Texture Diffuse;
		public Texture Normal;
		public Texture Gloss;
		public Texture Specular;

		public Material(string name, string parameters)
			: base(name, parameters)
		{
		}
	}
}
