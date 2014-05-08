using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Shaders
{
	public class Shader
	{
		public string VertexShader { get; set; }
		public string FragmentShader { get; set; }
		public List<string> Defines { get; set; }
	}
}
