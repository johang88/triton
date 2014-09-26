using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Materials
{
	class Material
	{
		public Dictionary<string, string> Samplers = new Dictionary<string, string>();
		public string ShaderSource { get; set; }
		public List<string> Defines { get; set; }
	}
}
